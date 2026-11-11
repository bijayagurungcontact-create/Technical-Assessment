import { inject, Injectable } from '@angular/core';
import { Book, StoreDetails } from '../domain/book';
import { BookRepository } from '../domain/book.repository';
import { NewBook } from '../domain/catalog';
import { BookListStore } from './book-list.store';

/**
 * Use-cases that change the store: add, update, delete. Each one persists through the
 * repository and then refreshes the shared list so every view (grid, genre counts) stays
 * in sync. Errors propagate as domain errors for the caller to present.
 */
@Injectable({ providedIn: 'root' })
export class ManageBooks {
  private readonly repository = inject(BookRepository);
  private readonly list = inject(BookListStore);

  async add(book: NewBook): Promise<Book> {
    const added = await this.repository.add(book);
    void this.list.refresh();
    return added;
  }

  async update(id: string, details: StoreDetails): Promise<Book> {
    const updated = await this.repository.update(id, details);
    void this.list.refresh();
    return updated;
  }

  async remove(id: string): Promise<void> {
    await this.repository.remove(id);
    void this.list.refresh();
  }
}
