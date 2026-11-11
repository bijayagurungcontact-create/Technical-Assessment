import { TestBed } from '@angular/core/testing';
import { CatalogBook } from '../domain/catalog';
import { CatalogRepository } from '../domain/catalog.repository';
import { CatalogUnavailableError } from '../domain/errors';
import { Page } from '../domain/paging';
import { CatalogSearch } from './catalog-search';

const dune: CatalogBook = {
  openLibraryKey: '/works/OL893414W',
  title: 'Dune',
  author: 'Frank Herbert',
  isbn: null,
  publishedYear: 1965,
  coverUrl: null,
  alreadyInStore: false,
};

describe('CatalogSearch', () => {
  let search: (term: string) => Promise<Page<CatalogBook>>;
  let calls: string[];
  let useCase: CatalogSearch;

  beforeEach(() => {
    calls = [];
    search = async () => ({ items: [dune], page: 1, pageSize: 10, totalCount: 1 });
    const repository: CatalogRepository = {
      search: (term: string) => {
        calls.push(term);
        return search(term);
      },
    };
    TestBed.configureTestingModule({
      providers: [CatalogSearch, { provide: CatalogRepository, useValue: repository }],
    });
    useCase = TestBed.inject(CatalogSearch);
  });

  it('does not call the API for terms shorter than the minimum', async () => {
    await useCase.search(' d ');
    expect(calls).toEqual([]);
    expect(useCase.status()).toBe('idle');
  });

  it('returns results for a valid term', async () => {
    await useCase.search('  dune ');
    expect(calls).toEqual(['dune']);
    expect(useCase.status()).toBe('success');
    expect(useCase.results()).toEqual([dune]);
  });

  it('reports "no results" distinctly from an error', async () => {
    search = async () => ({ items: [], page: 1, pageSize: 10, totalCount: 0 });
    await useCase.search('zzzz');
    expect(useCase.noResults()).toBe(true);
    expect(useCase.error()).toBeNull();
  });

  it('surfaces catalog outages and can retry', async () => {
    search = async () => {
      throw new CatalogUnavailableError();
    };
    await useCase.search('dune');
    expect(useCase.status()).toBe('error');
    expect(useCase.error()).toContain('Open Library is currently unavailable');

    search = async () => ({ items: [dune], page: 1, pageSize: 10, totalCount: 1 });
    await useCase.retry();
    expect(useCase.status()).toBe('success');
    expect(calls).toEqual(['dune', 'dune']);
  });

  it('does not repeat an identical successful search, but does after a reset', async () => {
    await useCase.search('dune');
    await useCase.search('dune');
    expect(calls).toEqual(['dune']);

    useCase.reset();
    await useCase.search('dune');
    expect(calls).toEqual(['dune', 'dune']);
  });
});
