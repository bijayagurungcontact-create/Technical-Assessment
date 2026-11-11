import { Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputNumber } from 'primeng/inputnumber';
import { Message } from 'primeng/message';
import { Select } from 'primeng/select';
import { Textarea } from 'primeng/textarea';
import { ManageBooks } from '../../../application/manage-books';
import { Book, Genre } from '../../../domain/book';
import { errorMessage, ValidationError } from '../../../domain/errors';
import { BookCover } from '../../shared/book-cover';
import { STORE_CURRENCY } from '../../shared/currency';
import { applyServerErrors, fieldError } from '../../shared/form-errors';
import { GENRE_OPTIONS } from '../../shared/genre-label.pipe';

/** Edits the store-owned fields of a book. Catalog fields are shown read-only. */
@Component({
  selector: 'app-edit-book-dialog',
  imports: [ReactiveFormsModule, Button, Dialog, InputNumber, Message, Select, Textarea, BookCover],
  templateUrl: './edit-book-dialog.html',
})
export class EditBookDialog {
  /** The book to edit; the dialog is open while this is non-null. */
  readonly book = input<Book | null>(null);
  readonly closed = output<void>();
  readonly updated = output<Book>();

  private readonly manageBooks = inject(ManageBooks);

  protected readonly open = computed(() => this.book() !== null);
  protected readonly genreOptions = GENRE_OPTIONS;
  protected readonly currency = STORE_CURRENCY;
  protected readonly fieldError = fieldError;
  protected readonly saving = signal(false);
  protected readonly saveError = signal<string | null>(null);

  protected readonly form = new FormGroup({
    genre: new FormControl<Genre>('Fiction', {
      nonNullable: true,
      validators: Validators.required,
    }),
    price: new FormControl<number | null>(null, [
      Validators.required,
      Validators.min(0),
      Validators.max(100_000),
    ]),
    stock: new FormControl(0, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(0)],
    }),
    description: new FormControl('', { nonNullable: true, validators: Validators.maxLength(4000) }),
  });

  constructor() {
    // Load the selected book into the form whenever a different book is opened.
    effect(() => {
      const book = this.book();
      if (book) {
        this.saveError.set(null);
        this.form.reset({
          genre: book.genre,
          price: book.price,
          stock: book.stock,
          description: book.description ?? '',
        });
      }
    });
  }

  protected async save(): Promise<void> {
    const book = this.book();
    this.form.markAllAsTouched();
    if (!book || this.form.invalid) {
      return;
    }

    const { genre, price, stock, description } = this.form.getRawValue();
    this.saving.set(true);
    this.saveError.set(null);

    try {
      const updated = await this.manageBooks.update(book.id, {
        genre,
        price: price!,
        stock,
        description: description || null,
      });
      this.updated.emit(updated);
      this.closed.emit();
    } catch (error) {
      const unmatched = error instanceof ValidationError ? applyServerErrors(this.form, error) : [];
      this.saveError.set(unmatched.length ? unmatched.join(' ') : errorMessage(error));
    } finally {
      this.saving.set(false);
    }
  }
}
