import { Book, GENRES, Genre, GenreCount, StoreDetails } from '../domain/book';
import { CatalogBook, NewBook } from '../domain/catalog';
import {
  AddBookRequestDto,
  BookDto,
  CatalogBookDto,
  GenreCountDto,
  UpdateBookRequestDto,
} from './api.dtos';

export function toBook(dto: BookDto): Book {
  return {
    id: dto.id,
    title: dto.title,
    author: dto.author,
    isbn: dto.isbn,
    publishedYear: dto.publishedYear,
    coverUrl: dto.coverUrl,
    openLibraryKey: dto.openLibraryKey,
    description: dto.description,
    genre: toGenre(dto.genre),
    price: dto.price,
    stock: dto.stock,
    addedAt: new Date(dto.addedAtUtc),
    updatedAt: dto.updatedAtUtc ? new Date(dto.updatedAtUtc) : null,
  };
}

export function toGenreCounts(dtos: readonly GenreCountDto[]): GenreCount[] {
  return dtos
    .filter((dto) => isGenre(dto.genre))
    .map((dto) => ({ genre: dto.genre as Genre, bookCount: dto.bookCount }));
}

export function toCatalogBook(dto: CatalogBookDto): CatalogBook {
  return { ...dto };
}

export function toAddBookRequest(book: NewBook): AddBookRequestDto {
  const { source } = book;
  return {
    openLibraryKey: source.openLibraryKey,
    title: source.title,
    author: source.author,
    isbn: source.isbn,
    publishedYear: source.publishedYear,
    coverUrl: source.coverUrl,
    ...toUpdateBookRequest(book),
  };
}

export function toUpdateBookRequest(details: StoreDetails): UpdateBookRequestDto {
  return {
    genre: details.genre,
    price: details.price,
    stock: details.stock,
    description: details.description?.trim() || null,
  };
}

function isGenre(value: string): value is Genre {
  return (GENRES as readonly string[]).includes(value);
}

function toGenre(value: string): Genre {
  if (!isGenre(value)) {
    throw new Error(`Unknown genre '${value}' received from the API.`);
  }
  return value;
}
