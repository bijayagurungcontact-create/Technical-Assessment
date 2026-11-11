// Wire format of the Book Store API. Only the infrastructure layer sees these types.

export interface BookDto {
  id: string;
  title: string;
  author: string;
  isbn: string | null;
  publishedYear: number | null;
  coverUrl: string | null;
  openLibraryKey: string;
  description: string | null;
  genre: string;
  price: number;
  stock: number;
  addedAtUtc: string;
  updatedAtUtc: string | null;
}

export interface PagedResultDto<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface GenreCountDto {
  genre: string;
  bookCount: number;
}

export interface AddBookRequestDto {
  openLibraryKey: string;
  title: string;
  author: string;
  isbn: string | null;
  publishedYear: number | null;
  coverUrl: string | null;
  description: string | null;
  genre: string;
  price: number;
  stock: number;
}

export interface UpdateBookRequestDto {
  genre: string;
  price: number;
  stock: number;
  description: string | null;
}

export interface CatalogBookDto {
  openLibraryKey: string;
  title: string;
  author: string;
  isbn: string | null;
  publishedYear: number | null;
  coverUrl: string | null;
  alreadyInStore: boolean;
}

/** RFC 7807 body returned by the API for every error. */
export interface ProblemDetailsDto {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
  correlationId?: string;
}
