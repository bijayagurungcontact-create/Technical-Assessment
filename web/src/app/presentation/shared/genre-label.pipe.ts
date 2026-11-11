import { Pipe, PipeTransform } from '@angular/core';
import { GENRES, Genre } from '../../domain/book';

const GENRE_LABELS: Record<Genre, string> = {
  Fiction: 'Fiction',
  SciFi: 'Sci-Fi',
  Fantasy: 'Fantasy',
  Mystery: 'Mystery',
  Biography: 'Biography',
  NonFiction: 'Non-fiction',
  Children: "Children's",
  History: 'History',
};

export function genreLabel(genre: Genre): string {
  return GENRE_LABELS[genre];
}

/** Options for PrimeNG selects, in the controlled-vocabulary order. */
export const GENRE_OPTIONS = GENRES.map((genre) => ({ label: genreLabel(genre), value: genre }));

@Pipe({ name: 'genreLabel' })
export class GenreLabelPipe implements PipeTransform {
  transform(genre: Genre): string {
    return genreLabel(genre);
  }
}
