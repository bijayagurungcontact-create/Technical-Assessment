# Book Store

A small, production-shaped book store. **Open Library is the catalog you search to *find* books;
the SQLite database is the store you *own*.** A user searches Open Library (through the API),
picks a real book, adds store-specific details (genre, price, stock) and the API persists it as
its own record. From then on Open Library is never queried for that book again.

```
/api   .NET 10 REST API — Clean Architecture + Vertical Slices, CQRS via a hand-rolled mediator, EF Core + SQLite
/web   Angular 21 client — Clean Architecture layers, Repository pattern, PrimeNG 21 + Tailwind CSS 4
```

---

## Running it from a clean checkout

### Prerequisites

| Tool | Version | Check |
|---|---|---|
| .NET SDK | 10.0.x | `dotnet --list-sdks` |
| Node.js | 20.19+ / 22.12+ / 24.x | `node --version` |
| npm | 10+ | `npm --version` |

No API keys or secrets are needed. Open Library is free and keyless.

### 1. API (http://localhost:5080)

```bash
cd api
dotnet run --project src/BookStore.Api
```

On first start the API applies the EF Core migration, creates `src/BookStore.Api/bookstore.db`
and seeds eight sample books. Delete that file to start over.

- OpenAPI document (Development only): http://localhost:5080/openapi/v1.json
- Tests: `dotnet test` (from `/api`)

### 2. Web (http://localhost:4200)

```bash
cd web
npm install
npm start
```

The dev server proxies `/api/*` to `http://localhost:5080` (`web/proxy.conf.json`), so the
browser only ever talks to its own origin. Start the API first.

- Tests: `npm test` (Vitest, run once)
- Production build: `npm run build`

### Configuration

Everything has a working default in `api/src/BookStore.Api/appsettings.json`. Any value can be
overridden with environment variables (`__` separates sections) or `appsettings.Development.json`:

| Setting | Default | Purpose |
|---|---|---|
| `ConnectionStrings__BookStore` | `Data Source=bookstore.db` | SQLite file (relative to the API project) |
| `OpenLibrary__BaseUrl` | `https://openlibrary.org/` | Search API |
| `OpenLibrary__CoversBaseUrl` | `https://covers.openlibrary.org/` | Used to derive `CoverUrl` |
| `OpenLibrary__UserAgent` | `BookStoreAssessment/1.0` | Open Library asks clients to identify themselves |
| `OpenLibrary__AttemptTimeoutSeconds` | `5` | Timeout per HTTP attempt |
| `OpenLibrary__TotalTimeoutSeconds` | `12` | Timeout for the whole call, including the retry |
| `Cors__AllowedOrigins__0` | `http://localhost:4200` | Only needed if the web app is served from another origin without the proxy |

The web app's API base URL is the `API_BASE_URL` injection token
(`web/src/app/infrastructure/api-config.ts`, default `/api`).

---

## API endpoints

| Method & route | Purpose | Touches Open Library? |
|---|---|---|
| `GET /api/catalog/search?q=&page=&pageSize=` | Search Open Library, return normalized, **transient** results (each flagged `alreadyInStore`) | **Yes** (proxied) |
| `GET /api/books?genre=&page=&pageSize=&sortBy=&sortDirection=` | Paged, sorted, filtered store books | No |
| `GET /api/books/{id}` | One store book | No |
| `POST /api/books` | Persist the selected search result + store fields | No |
| `PUT /api/books/{id}` | Update store-owned fields (genre, price, stock, description) | No |
| `DELETE /api/books/{id}` | Remove from the store | No |
| `GET /api/genres` | Every genre with its book count (powers the header filter) | No |

`sortBy` ∈ `title, author, publishedYear, genre, price, stock, addedAt`; `sortDirection` ∈ `asc, desc`.
Errors are always RFC 7807 `ProblemDetails` with a `correlationId`: 400 (validation, with
per-field `errors`), 404, 409 (duplicate), 503 (Open Library unavailable).

> **Hand-written SQL:** `api/src/BookStore.Infrastructure/Persistence/GenreStatistics.cs` — a raw
> `GROUP BY Genre` / `COUNT(*)` query against SQLite (via `Database.SqlQueryRaw`), exposed through
> the `GetGenreCounts` slice and `GET /api/genres`. The header dropdown uses it to show "Sci-Fi (3)".

---

## Architecture

### API: Clean Architecture *and* Vertical Slices

```
src/
  BookStore.Domain          Book entity (invariants, immutable catalog fields), Genre enum. No dependencies.
  BookStore.Application     Ports + use cases, organised BY FEATURE:
    Abstractions/           IBookStoreDbContext, IBookCatalog, IGenreStatistics (ports)
    Common/                 Mediator, pipeline behaviors (logging, validation), exceptions, PagedResult
    Features/
      Books/AddBook/        AddBookCommand · AddBookValidator · AddBookHandler
      Books/GetBooks/       GetBooksQuery · GetBooksValidator · GetBooksHandler · BookSorting
      Books/GetBookById/    ...
      Books/UpdateBook/     ...
      Books/DeleteBook/     ...
      Books/GetGenreCounts/ ...
      Catalog/SearchCatalog/SearchCatalogQuery · Validator · Handler
  BookStore.Infrastructure  EF Core/SQLite (DbContext, config, migration, seeder, raw SQL),
                            Open Library client (typed HttpClient + resilience)
  BookStore.Api             Minimal-API endpoints (thin adapters → ISender), ProblemDetails mapping,
                            correlation-id middleware, Serilog
```

**How the two ideas fit together.** The *layers* (projects) give the dependency rule:
Api → Infrastructure → Application → Domain, and nothing points outward. Inside the Application
layer the code is cut *by feature*, not by technical type: everything about "add a book" (the
request, its validation, its handler) sits in one folder and can change without touching other
slices. Clean Architecture supplies the boundaries; vertical slices organise what sits inside them.

Decisions worth calling out:

- **Hand-rolled mediator** (`Application/Common/Messaging`, ~60 lines) instead of MediatR. It is
  all this app needs: `ISender`, one handler per request, and ordered pipeline behaviors. It also
  avoids MediatR's commercial licence. `ICommand<T>` and `IQuery<T>` are marker interfaces so a
  request's intent is obvious from its type.
- **No generic repository on the API.** Handlers use `IBookStoreDbContext` (an Application port
  over EF Core). `DbContext` already is a unit of work plus repository, and each slice writes the
  exact query it needs, e.g. `ExecuteDeleteAsync` for deletes. Provider-specific SQL lives in
  Infrastructure behind `IGenreStatistics`.
- **Open Library is isolated behind `IBookCatalog`.** `OpenLibraryBookCatalog` builds the request
  (an ISBN-looking term uses `isbn=`, anything else `q=`), normalizes the response (joins authors,
  takes the first valid ISBN or the one searched for, derives `CoverUrl`), and converts every
  transport failure into `CatalogUnavailableException` → 503. Timeouts, one retry and a circuit
  breaker come from `Microsoft.Extensions.Http.Resilience`.
- **Search results are transient.** `SearchCatalog` writes nothing. It only reads the store to flag
  results that are already owned, so the UI can stop duplicates before the user picks one.
- **Duplicate prevention at two levels.** The `AddBook` handler checks for the same Open Library key
  or normalized ISBN and returns a friendly 409. Unique indexes are the real guarantee against races:
  a unique-constraint violation from SQLite is also mapped to a 409.
- **Catalog fields are immutable.** `Book` exposes only `UpdateStoreDetails(...)`, and `PUT` accepts
  only store fields.
- **Money in SQLite.** SQLite has no decimal type, and EF cannot `ORDER BY` a decimal stored as text,
  so `Price` is stored as integer cents (`PriceCents`). That keeps it exact and lets price sorting
  run server-side.
- **Validation** uses FluentValidation in a pipeline behavior. Every request is validated before its
  handler runs, and domain invariants back that up. `CoverUrl` must be `https://covers.openlibrary.org/…`
  so a client cannot make the app render arbitrary URLs.
- **Structured logging** uses Serilog with message templates (`{SearchTerm}`, `{ElapsedMs}`, `{BookId}`,
  …). `CorrelationIdMiddleware` accepts or creates an `X-Correlation-ID`, returns it in the response,
  includes it in error bodies, and pushes it into the log context. Every line of a request carries it,
  including the outbound Open Library call and its retries:

  ```
  [10:37:59 ERR] [outage-demo-1] Polly: Resilience event occurred. EventName: 'OnTimeout' ...
  [10:38:02 WRN] [outage-demo-1] ...OpenLibraryBookCatalog: Open Library search for dune failed after 4995 ms
  [10:38:02 ERR] [outage-demo-1] ...RequestLoggingMiddleware: HTTP GET /api/catalog/search responded 503 in 5230 ms
  ```

### Web: Clean Architecture in the folder structure

```
src/app/
  domain/           Book, Genre, CatalogBook, paging types, domain errors, and the ports
                    BookRepository / CatalogRepository. Plain TypeScript: no Angular, HTTP or RxJS imports.
  application/      Use cases + state: BookListStore (list, filter, paging, genre counts),
                    CatalogSearch (search Open Library), ManageBooks (add/update/delete). Depend only on domain ports.
  infrastructure/   BookHttpRepository, CatalogHttpRepository, DTOs, DTO↔model mappers, HTTP→domain
                    error mapping, provideInfrastructure() DI wiring. The only layer that knows HttpClient or URLs.
  presentation/     Header, Body, Footer (layout), BookGrid, AddBookDialog, EditBookDialog, shared bits.
                    PrimeNG components, Tailwind for layout.
  app.ts / app.html The shell: exactly <app-header/> <app-body/> <app-footer/>.
```

- **Repository pattern.** The ports are *abstract classes* in `domain/`. An abstract class can be the
  Angular DI token without the domain importing Angular (an `InjectionToken` would need that import).
  `provideInfrastructure()` binds them to the HTTP implementations. Repositories return `Promise`s
  and reject with domain errors (`DuplicateBookError`, `ValidationError`, `CatalogUnavailableError`, …),
  so nothing above infrastructure ever sees an `HttpErrorResponse`.
- **Header → Body communication (siblings).** Both inject the root-provided `BookListStore`, a
  signal-based store in the application layer. The Header calls `selectGenre()`, which also resets
  to page 1. The Body reads `books()`, `loading()`, `error()` and `query()`. Why this approach:
  - it uses a single source of truth that is independent of the component tree, so neither
    component knows the other exists;
  - signals fit the zoneless change detection Angular 21 uses by default;
  - the same store holds paging, sorting and genre counts, so add/edit/delete just call `refresh()`
    and the grid and header counts both update;
  - it is unit-testable with a fake repository and no DOM.

  Alternatives considered: a bare `Subject` service (more manual subscription handling, same idea)
  and URL query parameters (great for deep links; see "What I'd do next").
- **Server-side paging and sorting.** `p-table` runs in `lazy` mode, and the API does the work.
- **Add-book flow** is two steps in one PrimeNG dialog. Step 1 searches (debounced, or Enter), with
  idle, loading, empty, "Open Library unavailable + Try again" and "In store" (disabled) states.
  Step 2 collects genre, price and stock, then saves. API validation errors are attached to the
  matching form fields.
- **States everywhere.** The grid shows loading, empty (with a genre-aware message), and error
  (with Retry). Stale responses are dropped when filters change quickly. If deleting the last row
  leaves an empty page, the grid steps back a page.

---

## Tests

**API: 37 xUnit tests** (`api/tests/BookStore.UnitTests`)
- Slices run through the **real mediator pipeline** (validation behavior included) against
  **in-memory SQLite**, so unique indexes, value converters and the raw SQL behave as in production.
  Covered: add plus both duplicate paths, invalid price/cover/key, genre filter, server-side price
  sort with paging, default ordering, rejected sort fields, update/delete including 404, genre counts
  (raw SQL), and search flagging `alreadyInStore` without persisting anything.
- `OpenLibraryBookCatalog` against a stubbed `HttpMessageHandler`: normalization, ISBN-vs-text
  query, cover fallback, and 500, malformed JSON and timeouts all → `CatalogUnavailableException`.
- Mediator: behavior ordering and short-circuit on validation failure.
- Domain: ISBN normalization and invariants.

**Web: 20 Vitest tests**
- `BookListStore`: genre change resets to page 1, stale responses ignored, error then recovery,
  step-back on empty page, totals.
- `CatalogSearch`: min-length guard, no-results vs error, retry, de-duplication.
- `BookHttpRepository` (HttpTestingController): query parameters, DTO→model mapping, POST body,
  ProblemDetails → domain errors.
- `toDomainError` mapping, and an **app-shell test** asserting the root renders exactly
  Header, Body and Footer as direct siblings.

---

## Trade-offs and what I'd do next

- **Catalog fields are trusted from the client on `POST`.** The client echoes back the search result
  it picked. Re-fetching from Open Library on save would break the rule that Open Library is used
  only for search, and add latency. The fields are validated: key format, ISBN shape, lengths, and
  cover URL host. A stricter option is to sign the search results or cache them server-side for a
  few minutes.
- **No optimistic concurrency** on `PUT`. Two simultaneous edits mean last write wins. Next step: a
  row version and `If-Match`.
- **Description** is optional and entered by hand. The Works endpoint fetch was intentionally skipped.
- **Integration tests over HTTP** (`WebApplicationFactory`) for ProblemDetails shapes and status codes
  would be my next tests. Today the status codes were checked manually with curl, and the handler
  tests cover the logic behind them.
- **Web:** sync the genre, page and sort to URL query parameters for deep links and back-button
  support. Add an ESLint boundaries rule so the layer dependency direction is enforced automatically,
  not just by convention. Add an e2e smoke test (Playwright) in CI.
- **Currency** is fixed to USD (`presentation/shared/currency.ts`).
- Search pagination in the add dialog is limited to the top 10 results. Refining the search term
  works better for this UX than paging.
