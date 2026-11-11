import { computed, inject, Injectable, signal } from '@angular/core';
import { CatalogBook } from '../domain/catalog';
import { CatalogRepository } from '../domain/catalog.repository';
import { errorMessage } from '../domain/errors';

export type SearchStatus = 'idle' | 'loading' | 'success' | 'error';

export const MIN_SEARCH_LENGTH = 2;
const PAGE_SIZE = 10;

/**
 * Use-case: search the external catalog for a book to add. Results are transient — nothing
 * here is persisted. Not root-provided: the add-book dialog provides its own instance and
 * resets it whenever the dialog closes.
 */
@Injectable()
export class CatalogSearch {
  private readonly repository = inject(CatalogRepository);

  private readonly _term = signal('');
  private readonly _results = signal<readonly CatalogBook[]>([]);
  private readonly _totalCount = signal(0);
  private readonly _status = signal<SearchStatus>('idle');
  private readonly _error = signal<string | null>(null);
  private latestRequest = 0;

  readonly term = this._term.asReadonly();
  readonly results = this._results.asReadonly();
  readonly totalCount = this._totalCount.asReadonly();
  readonly status = this._status.asReadonly();
  readonly error = this._error.asReadonly();
  readonly noResults = computed(() => this._status() === 'success' && this._results().length === 0);

  async search(rawTerm: string): Promise<void> {
    const term = rawTerm.trim();
    if (term.length < MIN_SEARCH_LENGTH) {
      this.reset();
      return;
    }
    // Typing + Enter (or a debounce firing after Enter) would otherwise repeat the same call.
    const status = this._status();
    if (term === this._term() && (status === 'loading' || status === 'success')) {
      return;
    }

    const request = ++this.latestRequest;
    this._term.set(term);
    this._status.set('loading');
    this._error.set(null);

    try {
      const page = await this.repository.search(term, 1, PAGE_SIZE);
      if (request !== this.latestRequest) {
        return;
      }
      this._results.set(page.items);
      this._totalCount.set(page.totalCount);
      this._status.set('success');
    } catch (error) {
      if (request !== this.latestRequest) {
        return;
      }
      this._results.set([]);
      this._error.set(errorMessage(error));
      this._status.set('error');
    }
  }

  retry(): Promise<void> {
    return this.search(this._term());
  }

  reset(): void {
    this.latestRequest++;
    this._term.set('');
    this._results.set([]);
    this._totalCount.set(0);
    this._status.set('idle');
    this._error.set(null);
  }
}
