import { Genre } from './book';

export type SortDirection = 'asc' | 'desc';

export const BOOK_SORT_FIELDS = [
  'title',
  'author',
  'publishedYear',
  'genre',
  'price',
  'stock',
  'addedAt',
] as const;

export type BookSortField = (typeof BOOK_SORT_FIELDS)[number];

export interface BookQuery {
  readonly genre: Genre | null;
  readonly page: number;
  readonly pageSize: number;
  readonly sortBy: BookSortField;
  readonly sortDirection: SortDirection;
}

export interface Page<T> {
  readonly items: readonly T[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalCount: number;
}
