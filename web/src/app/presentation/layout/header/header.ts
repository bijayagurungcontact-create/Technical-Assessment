import { Component, computed, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Select } from 'primeng/select';
import { BookListStore } from '../../../application/book-list.store';
import { Genre } from '../../../domain/book';
import { genreLabel } from '../../shared/genre-label.pipe';

const ALL_GENRES = 'all';
type GenreFilter = Genre | typeof ALL_GENRES;

/**
 * Owns the genre filter. Writes the selection into the shared {@link BookListStore};
 * the Body (a sibling) reacts to it. No @Input/@Output between them.
 */
@Component({
  selector: 'app-header',
  imports: [FormsModule, Select],
  templateUrl: './header.html',
})
export class Header implements OnInit {
  protected readonly store = inject(BookListStore);

  protected readonly selectedFilter = computed<GenreFilter>(
    () => this.store.selectedGenre() ?? ALL_GENRES,
  );

  protected readonly filterOptions = computed(() => [
    { label: `All genres (${this.store.storeTotal()})`, value: ALL_GENRES },
    ...this.store.genreCounts().map(({ genre, bookCount }) => ({
      label: `${genreLabel(genre)} (${bookCount})`,
      value: genre,
    })),
  ]);

  ngOnInit(): void {
    void this.store.loadGenreCounts();
  }

  protected onFilterChange(filter: GenreFilter): void {
    void this.store.selectGenre(filter === ALL_GENRES ? null : filter);
  }
}
