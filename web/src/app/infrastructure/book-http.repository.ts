import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Book, GenreCount, StoreDetails } from '../domain/book';
import { BookRepository } from '../domain/book.repository';
import { NewBook } from '../domain/catalog';
import { BookQuery, Page } from '../domain/paging';
import { BookDto, GenreCountDto, PagedResultDto } from './api.dtos';
import { API_BASE_URL } from './api-config';
import { toAddBookRequest, toBook, toGenreCounts, toUpdateBookRequest } from './book.mapper';
import { send } from './send';

@Injectable()
export class BookHttpRepository extends BookRepository {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  async list(query: BookQuery): Promise<Page<Book>> {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize)
      .set('sortBy', query.sortBy)
      .set('sortDirection', query.sortDirection);
    if (query.genre) {
      params = params.set('genre', query.genre);
    }

    const dto = await send(
      this.http.get<PagedResultDto<BookDto>>(`${this.baseUrl}/books`, { params }),
    );
    return { ...dto, items: dto.items.map(toBook) };
  }

  async genreCounts(): Promise<readonly GenreCount[]> {
    return toGenreCounts(await send(this.http.get<GenreCountDto[]>(`${this.baseUrl}/genres`)));
  }

  async add(book: NewBook): Promise<Book> {
    return toBook(
      await send(this.http.post<BookDto>(`${this.baseUrl}/books`, toAddBookRequest(book))),
    );
  }

  async update(id: string, details: StoreDetails): Promise<Book> {
    const url = `${this.baseUrl}/books/${encodeURIComponent(id)}`;
    return toBook(await send(this.http.put<BookDto>(url, toUpdateBookRequest(details))));
  }

  async remove(id: string): Promise<void> {
    await send(this.http.delete<void>(`${this.baseUrl}/books/${encodeURIComponent(id)}`));
  }
}
