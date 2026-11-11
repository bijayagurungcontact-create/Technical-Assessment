import { StoreDetails } from './book';

/** A transient search hit from the external catalog (Open Library). Not a store book. */
export interface CatalogBook {
  readonly openLibraryKey: string;
  readonly title: string;
  readonly author: string;
  readonly isbn: string | null;
  readonly publishedYear: number | null;
  readonly coverUrl: string | null;
  /** True when the store already owns this work, so it cannot be added again. */
  readonly alreadyInStore: boolean;
}

/** What gets persisted when a user adds a book: the catalog pick + the store's own fields. */
export interface NewBook extends StoreDetails {
  readonly source: CatalogBook;
}
