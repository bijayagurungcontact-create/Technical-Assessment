import { Component, computed, inject, signal } from '@angular/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { Button } from 'primeng/button';
import { ConfirmDialog } from 'primeng/confirmdialog';
import { Message } from 'primeng/message';
import { Toast } from 'primeng/toast';
import { BookListStore } from '../../../application/book-list.store';
import { ManageBooks } from '../../../application/manage-books';
import { Book } from '../../../domain/book';
import { errorMessage } from '../../../domain/errors';
import { AddBookDialog } from '../../books/add-book-dialog/add-book-dialog';
import { BookGrid, GridPageChange } from '../../books/book-grid/book-grid';
import { EditBookDialog } from '../../books/edit-book-dialog/edit-book-dialog';
import { genreLabel } from '../../shared/genre-label.pipe';

/**
 * The books page. Reads the list (already filtered by whatever genre the Header selected)
 * from the shared {@link BookListStore} and hosts the add / edit / delete interactions.
 */
@Component({
  selector: 'app-body',
  imports: [Button, ConfirmDialog, Message, Toast, AddBookDialog, BookGrid, EditBookDialog],
  providers: [ConfirmationService, MessageService],
  templateUrl: './body.html',
  host: { class: 'flex-1' },
})
export class Body {
  protected readonly store = inject(BookListStore);
  private readonly manageBooks = inject(ManageBooks);
  private readonly confirmation = inject(ConfirmationService);
  private readonly messages = inject(MessageService);

  protected readonly addDialogOpen = signal(false);
  protected readonly editing = signal<Book | null>(null);
  protected readonly deletingId = signal<string | null>(null);

  protected readonly subtitle = computed(() => {
    const genre = this.store.selectedGenre();
    return genre ? `Showing ${genreLabel(genre)} only` : 'Showing all genres';
  });

  protected onPageChange({ page, pageSize, sortBy, sortDirection }: GridPageChange): void {
    void this.store.changePage(page, pageSize, sortBy, sortDirection);
  }

  protected onAdded(book: Book): void {
    this.notify('success', 'Book added', `"${book.title}" is now in the store.`);
  }

  protected onUpdated(book: Book): void {
    this.notify('success', 'Book updated', `Saved changes to "${book.title}".`);
  }

  protected confirmDelete(book: Book): void {
    this.confirmation.confirm({
      header: 'Delete book',
      message: `Remove "${book.title}" from the store? This cannot be undone.`,
      icon: 'pi pi-exclamation-triangle',
      acceptButtonProps: { label: 'Delete', severity: 'danger' },
      rejectButtonProps: { label: 'Cancel', severity: 'secondary', outlined: true },
      accept: () => this.delete(book),
    });
  }

  private async delete(book: Book): Promise<void> {
    this.deletingId.set(book.id);
    try {
      await this.manageBooks.remove(book.id);
      this.notify('success', 'Book deleted', `"${book.title}" was removed from the store.`);
    } catch (error) {
      this.notify('error', 'Could not delete book', errorMessage(error));
    } finally {
      this.deletingId.set(null);
    }
  }

  private notify(severity: 'success' | 'error', summary: string, detail: string): void {
    this.messages.add({ severity, summary, detail, life: 4000 });
  }
}
