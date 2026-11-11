import { computed, inject, Injectable, signal } from '@angular/core';
import { Book, Genre, GenreCount } from '../domain/book';
import { BookRepository } from '../domain/book.repository';
import { errorMessage } from '../domain/errors';
import { BookQuery, BookSortField, Page, SortDirection } from '../domain/paging';

export const DEFAULT_BOOK_QUERY: BookQuery = {
  genre: null,
  page: 1,
  pageSize: 10,
  sortBy: 'addedAt',
  sortDirection: 'desc',
};

/**
 * Single source of truth for the store's book list: the active query (genre, page, sort),
 * the loaded page, and the genre counts.
 *
 * This is how the sibling Header and Body components communicate: both inject this one
 * root-scoped store. The Header *writes* the genre via {@link selectGenre}; the Body *reads*
 * {@link books}. Neither knows the other exists, and signals propagate changes without
 * manual subscriptions (the app runs zoneless).
 */
@Injectable({ providedIn: 'root' })
export class BookListStore {
  private readonly repository = inject(BookRepository);

  private readonly _query = signal<BookQuery>(DEFAULT_BOOK_QUERY);
  private readonly _page = signal<Page<Book> | null>(null);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _genreCounts = signal<readonly GenreCount[]>([]);

  /** Guards against out-of-order responses when the user changes filters quickly. */
  private latestRequest = 0;

  readonly query = this._query.asReadonly();
  readonly selectedGenre = computed(() => this._query().genre);
  readonly books = computed(() => this._page()?.items ?? []);
  readonly totalCount = computed(() => this._page()?.totalCount ?? 0);
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly genreCounts = this._genreCounts.asReadonly();
  readonly storeTotal = computed(() =>
    this._genreCounts().reduce((sum, { bookCount }) => sum + bookCount, 0),
  );

  selectGenre(genre: Genre | null): Promise<void> {
    if (genre === this._query().genre) {
      return Promise.resolve();
    }
    // A new filter always starts from the first page.
    return this.updateQuery({ genre, page: 1 });
  }

  changePage(
    page: number,
    pageSize: number,
    sortBy: BookSortField,
    sortDirection: SortDirection,
  ): Promise<void> {
    return this.updateQuery({ page, pageSize, sortBy, sortDirection });
  }

  /** Reload the grid and the genre counts, e.g. after a book was added, edited or deleted. */
  async refresh(): Promise<void> {
    await Promise.all([this.load(), this.loadGenreCounts()]);
  }

  async loadGenreCounts(): Promise<void> {
    try {
      this._genreCounts.set(await this.repository.genreCounts());
    } catch {
      // Non-critical: the filter still works without counts, and the grid reports API errors.
    }
  }

  async load(): Promise<void> {
    const request = ++this.latestRequest;
    const query = this._query();
    this._loading.set(true);
    this._error.set(null);

    try {
      const page = await this.repository.list(query);
      if (request !== this.latestRequest) {
        return;
      }

      // e.g. the last book on the last page was deleted: step back to the new last page.
      if (page.items.length === 0 && page.totalCount > 0 && query.page > 1) {
        const lastPage = Math.ceil(page.totalCount / query.pageSize);
        this._query.set({ ...query, page: lastPage });
        return this.load();
      }

      this._page.set(page);
    } catch (error) {
      if (request === this.latestRequest) {
        this._error.set(errorMessage(error));
      }
    } finally {
      if (request === this.latestRequest) {
        this._loading.set(false);
      }
    }
  }

  private updateQuery(changes: Partial<BookQuery>): Promise<void> {
    this._query.update((query) => ({ ...query, ...changes }));
    return this.load();
  }
}
