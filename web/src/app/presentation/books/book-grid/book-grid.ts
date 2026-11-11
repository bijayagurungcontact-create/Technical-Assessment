import { CurrencyPipe } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { Button } from 'primeng/button';
import { TableLazyLoadEvent, TableModule } from 'primeng/table';
import { Tag } from 'primeng/tag';
import { Tooltip } from 'primeng/tooltip';
import { Book } from '../../../domain/book';
import { BOOK_SORT_FIELDS, BookQuery, BookSortField, SortDirection } from '../../../domain/paging';
import { BookCover } from '../../shared/book-cover';
import { STORE_CURRENCY } from '../../shared/currency';
import { GenreLabelPipe } from '../../shared/genre-label.pipe';

export interface GridPageChange {
  page: number;
  pageSize: number;
  sortBy: BookSortField;
  sortDirection: SortDirection;
}

/**
 * Presentational grid: renders whatever page it is given and reports paging/sorting changes.
 * Paging and sorting are server-side (lazy table) — the API does the work.
 */
@Component({
  selector: 'app-book-grid',
  imports: [CurrencyPipe, Button, TableModule, Tag, Tooltip, BookCover, GenreLabelPipe],
  templateUrl: './book-grid.html',
})
export class BookGrid {
  readonly books = input.required<readonly Book[]>();
  readonly totalCount = input.required<number>();
  readonly loading = input.required<boolean>();
  readonly query = input.required<BookQuery>();
  readonly deletingId = input<string | null>(null);

  readonly pageChange = output<GridPageChange>();
  readonly edit = output<Book>();
  readonly delete = output<Book>();
  readonly add = output<void>();

  protected readonly currency = STORE_CURRENCY;
  protected readonly rowsPerPageOptions = [5, 10, 25, 50];

  protected onLazyLoad(event: TableLazyLoadEvent): void {
    const pageSize = event.rows ?? this.query().pageSize;
    const sortField = typeof event.sortField === 'string' ? event.sortField : null;

    this.pageChange.emit({
      page: Math.floor((event.first ?? 0) / pageSize) + 1,
      pageSize,
      sortBy: isSortField(sortField) ? sortField : this.query().sortBy,
      sortDirection: event.sortOrder === 1 ? 'asc' : 'desc',
    });
  }

  // Typed accessor for the row template (PrimeNG's let-row context is untyped).
  protected asBook(row: unknown): Book {
    return row as Book;
  }
}

function isSortField(value: string | null): value is BookSortField {
  return value !== null && (BOOK_SORT_FIELDS as readonly string[]).includes(value);
}
