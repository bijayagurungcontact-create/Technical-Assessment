import { TestBed } from '@angular/core/testing';
import { Book, GenreCount } from '../domain/book';
import { BookRepository } from '../domain/book.repository';
import { ServerUnreachableError } from '../domain/errors';
import { BookQuery, Page } from '../domain/paging';
import { BookListStore } from './book-list.store';

/** In-memory stand-in for the HTTP repository: the store only depends on the port. */
class FakeBookRepository extends BookRepository {
  readonly queries: BookQuery[] = [];
  pending: Array<{ resolve: (page: Page<Book>) => void; reject: (e: unknown) => void }> = [];

  list(query: BookQuery): Promise<Page<Book>> {
    this.queries.push(query);
    return new Promise((resolve, reject) => this.pending.push({ resolve, reject }));
  }
  genreCounts(): Promise<readonly GenreCount[]> {
    return Promise.resolve([
      { genre: 'Fiction', bookCount: 2 },
      { genre: 'SciFi', bookCount: 3 },
    ]);
  }
  add = () => Promise.reject(new Error('not used'));
  update = () => Promise.reject(new Error('not used'));
  remove = () => Promise.reject(new Error('not used'));
}

function page(titles: string[], totalCount = titles.length, pageNumber = 1): Page<Book> {
  return {
    items: titles.map((title, i) => ({ id: String(i), title }) as Book),
    page: pageNumber,
    pageSize: 10,
    totalCount,
  };
}

describe('BookListStore', () => {
  let repository: FakeBookRepository;
  let store: BookListStore;

  beforeEach(() => {
    repository = new FakeBookRepository();
    TestBed.configureTestingModule({
      providers: [{ provide: BookRepository, useValue: repository }],
    });
    store = TestBed.inject(BookListStore);
  });

  it('selecting a genre filters the list and goes back to the first page', async () => {
    const firstLoad = store.changePage(3, 10, 'title', 'asc');
    repository.pending.shift()!.resolve(page(['A'], 30, 3));
    await firstLoad;

    const filtered = store.selectGenre('SciFi');
    repository.pending.shift()!.resolve(page(['Dune']));
    await filtered;

    expect(repository.queries.at(-1)).toEqual({
      genre: 'SciFi',
      page: 1,
      pageSize: 10,
      sortBy: 'title',
      sortDirection: 'asc',
    });
    expect(store.selectedGenre()).toBe('SciFi');
    expect(store.books().map((b) => b.title)).toEqual(['Dune']);
  });

  it('ignores a slow response that was superseded by a newer request', async () => {
    const slow = store.selectGenre('Fiction');
    const fast = store.selectGenre('SciFi');

    repository.pending[1].resolve(page(['Dune']));
    await fast;
    repository.pending[0].resolve(page(['Stale fiction']));
    await slow;

    expect(store.books().map((b) => b.title)).toEqual(['Dune']);
    expect(store.loading()).toBe(false);
  });

  it('exposes a readable error and clears it on the next successful load', async () => {
    const failing = store.load();
    repository.pending.shift()!.reject(new ServerUnreachableError());
    await failing;

    expect(store.error()).toContain('Cannot reach');
    expect(store.loading()).toBe(false);

    const retry = store.load();
    repository.pending.shift()!.resolve(page(['Dune']));
    await retry;

    expect(store.error()).toBeNull();
  });

  it('steps back a page when the current page became empty (e.g. after a delete)', async () => {
    const load = store.changePage(3, 10, 'title', 'asc');
    repository.pending.shift()!.resolve(page([], 15, 3));
    await new Promise((resolve) => setTimeout(resolve)); // let the store issue its follow-up request
    repository.pending.shift()!.resolve(page(['Last'], 15, 2));
    await load;

    expect(store.query().page).toBe(2);
    expect(store.books().map((b) => b.title)).toEqual(['Last']);
  });

  it('totals the genre counts for the "All" option', async () => {
    await store.loadGenreCounts();
    expect(store.storeTotal()).toBe(5);
  });
});
