import { Book, GenreCount, StoreDetails } from './book';
import { NewBook } from './catalog';
import { BookQuery, Page } from './paging';

/**
 * Port for the store's own books. Declared as an abstract class (not an interface) so it can
 * double as the Angular DI token without this file importing anything from Angular.
 * Implementations reject with a {@link BookStoreError}.
 */
export abstract class BookRepository {
  abstract list(query: BookQuery): Promise<Page<Book>>;
  abstract genreCounts(): Promise<readonly GenreCount[]>;
  abstract add(book: NewBook): Promise<Book>;
  abstract update(id: string, details: StoreDetails): Promise<Book>;
  abstract remove(id: string): Promise<void>;
}
