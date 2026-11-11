// Domain layer: plain TypeScript only — no Angular, no HTTP, no RxJS.

/** Controlled vocabulary, identical to the API's Genre enum. */
export const GENRES = [
  'Fiction',
  'SciFi',
  'Fantasy',
  'Mystery',
  'Biography',
  'NonFiction',
  'Children',
  'History',
] as const;

export type Genre = (typeof GENRES)[number];

/** A book owned by the store. */
export interface Book {
  readonly id: string;
  // Captured from Open Library when the book was added; never changes afterwards.
  readonly title: string;
  readonly author: string;
  readonly isbn: string | null;
  readonly publishedYear: number | null;
  readonly coverUrl: string | null;
  readonly openLibraryKey: string;
  // Owned by the store.
  readonly description: string | null;
  readonly genre: Genre;
  readonly price: number;
  readonly stock: number;
  readonly addedAt: Date;
  readonly updatedAt: Date | null;
}

/** The fields the store sets and may later edit. */
export interface StoreDetails {
  readonly genre: Genre;
  readonly price: number;
  readonly stock: number;
  readonly description: string | null;
}

export interface GenreCount {
  readonly genre: Genre;
  readonly bookCount: number;
}
