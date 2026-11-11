import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { CatalogBook } from '../domain/catalog';
import { CatalogRepository } from '../domain/catalog.repository';
import { Page } from '../domain/paging';
import { CatalogBookDto, PagedResultDto } from './api.dtos';
import { API_BASE_URL } from './api-config';
import { toCatalogBook } from './book.mapper';
import { send } from './send';

/** Searches Open Library *through our API* — the browser never talks to Open Library directly. */
@Injectable()
export class CatalogHttpRepository extends CatalogRepository {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  async search(term: string, page: number, pageSize: number): Promise<Page<CatalogBook>> {
    const params = new HttpParams().set('q', term).set('page', page).set('pageSize', pageSize);
    const dto = await send(
      this.http.get<PagedResultDto<CatalogBookDto>>(`${this.baseUrl}/catalog/search`, { params }),
    );
    return { ...dto, items: dto.items.map(toCatalogBook) };
  }
}
