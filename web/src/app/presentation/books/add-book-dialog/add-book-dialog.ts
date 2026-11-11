import { Component, inject, model, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { IconField } from 'primeng/iconfield';
import { InputIcon } from 'primeng/inputicon';
import { InputNumber } from 'primeng/inputnumber';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { ProgressSpinner } from 'primeng/progressspinner';
import { Select } from 'primeng/select';
import { Tag } from 'primeng/tag';
import { Textarea } from 'primeng/textarea';
import { debounceTime } from 'rxjs';
import { CatalogSearch, MIN_SEARCH_LENGTH } from '../../../application/catalog-search';
import { ManageBooks } from '../../../application/manage-books';
import { Book, Genre } from '../../../domain/book';
import { CatalogBook } from '../../../domain/catalog';
import { errorMessage, ValidationError } from '../../../domain/errors';
import { BookCover } from '../../shared/book-cover';
import { STORE_CURRENCY } from '../../shared/currency';
import { applyServerErrors, fieldError } from '../../shared/form-errors';
import { GENRE_OPTIONS } from '../../shared/genre-label.pipe';

const SEARCH_DEBOUNCE_MS = 400;

/**
 * Two-step add flow: (1) search Open Library through the API and pick a result,
 * (2) set the store-owned fields (genre, price, stock) and save.
 */
@Component({
  selector: 'app-add-book-dialog',
  imports: [
    ReactiveFormsModule,
    Button,
    Dialog,
    IconField,
    InputIcon,
    InputNumber,
    InputText,
    Message,
    ProgressSpinner,
    Select,
    Tag,
    Textarea,
    BookCover,
  ],
  providers: [CatalogSearch],
  templateUrl: './add-book-dialog.html',
})
export class AddBookDialog {
  readonly visible = model(false);
  readonly added = output<Book>();

  protected readonly search = inject(CatalogSearch);
  private readonly manageBooks = inject(ManageBooks);

  protected readonly minSearchLength = MIN_SEARCH_LENGTH;
  protected readonly genreOptions = GENRE_OPTIONS;
  protected readonly currency = STORE_CURRENCY;
  protected readonly fieldError = fieldError;

  protected readonly selected = signal<CatalogBook | null>(null);
  protected readonly saving = signal(false);
  protected readonly saveError = signal<string | null>(null);

  protected readonly searchControl = new FormControl('', { nonNullable: true });
  protected readonly form = new FormGroup({
    genre: new FormControl<Genre | null>(null, Validators.required),
    price: new FormControl<number | null>(null, [
      Validators.required,
      Validators.min(0),
      Validators.max(100_000),
    ]),
    stock: new FormControl(1, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(0)],
    }),
    description: new FormControl('', { nonNullable: true, validators: Validators.maxLength(4000) }),
  });

  constructor() {
    // Search as the user types (debounced); Enter searches immediately.
    // Repeated terms are de-duplicated by CatalogSearch itself, which also knows about resets.
    this.searchControl.valueChanges
      .pipe(debounceTime(SEARCH_DEBOUNCE_MS), takeUntilDestroyed())
      .subscribe((term) => void this.search.search(term));
  }

  protected searchNow(): void {
    void this.search.search(this.searchControl.value);
  }

  protected select(book: CatalogBook): void {
    if (book.alreadyInStore) {
      return;
    }
    this.selected.set(book);
    this.saveError.set(null);
  }

  protected backToResults(): void {
    this.selected.set(null);
    this.saveError.set(null);
  }

  protected async save(): Promise<void> {
    const source = this.selected();
    this.form.markAllAsTouched();
    if (!source || this.form.invalid) {
      return;
    }

    const { genre, price, stock, description } = this.form.getRawValue();
    this.saving.set(true);
    this.saveError.set(null);

    try {
      const book = await this.manageBooks.add({
        source,
        genre: genre!,
        price: price!,
        stock,
        description: description || null,
      });
      this.added.emit(book);
      this.visible.set(false);
    } catch (error) {
      const unmatched = error instanceof ValidationError ? applyServerErrors(this.form, error) : [];
      this.saveError.set(unmatched.length ? unmatched.join(' ') : errorMessage(error));
    } finally {
      this.saving.set(false);
    }
  }

  /** Every time the dialog closes, start fresh next time. */
  protected reset(): void {
    this.search.reset();
    this.searchControl.setValue('', { emitEvent: false });
    this.selected.set(null);
    this.saveError.set(null);
    this.form.reset({ genre: null, price: null, stock: 1, description: '' });
  }
}
