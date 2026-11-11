import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { BookRepository } from '../domain/book.repository';
import { BookNotFoundError, ValidationError } from '../domain/errors';
import { BookDto } from './api.dtos';
import { BookHttpRepository } from './book-http.repository';

const duneDto: BookDto = {
  id: 'b1',
  title: 'Dune',
  author: 'Frank Herbert',
  isbn: '9780441172719',
  publishedYear: 1965,
  coverUrl: null,
  openLibraryKey: '/works/OL893414W',
  description: null,
  genre: 'SciFi',
  price: 18.99,
  stock: 3,
  addedAtUtc: '2026-01-01T10:00:00Z',
  updatedAtUtc: null,
};

describe('BookHttpRepository', () => {
  let repository: BookRepository;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: BookRepository, useClass: BookHttpRepository },
      ],
    });
    repository = TestBed.inject(BookRepository);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('sends the query as API parameters and maps DTOs to domain books', async () => {
    const result = repository.list({
      genre: 'SciFi',
      page: 2,
      pageSize: 5,
      sortBy: 'price',
      sortDirection: 'desc',
    });

    const request = http.expectOne((r) => r.url === '/api/books');
    expect(request.request.params.toString()).toBe(
      'page=2&pageSize=5&sortBy=price&sortDirection=desc&genre=SciFi',
    );
    request.flush({ items: [duneDto], page: 2, pageSize: 5, totalCount: 6 });

    const page = await result;
    expect(page.totalCount).toBe(6);
    expect(page.items[0].genre).toBe('SciFi');
    expect(page.items[0].addedAt).toEqual(new Date('2026-01-01T10:00:00Z'));
  });

  it('omits the genre parameter for "All"', async () => {
    const result = repository.list({
      genre: null,
      page: 1,
      pageSize: 10,
      sortBy: 'addedAt',
      sortDirection: 'desc',
    });
    const request = http.expectOne((r) => r.url === '/api/books');
    expect(request.request.params.has('genre')).toBe(false);
    request.flush({ items: [], page: 1, pageSize: 10, totalCount: 0 });
    await result;
  });

  it('posts the catalog pick together with the store fields', async () => {
    const result = repository.add({
      source: {
        openLibraryKey: '/works/OL893414W',
        title: 'Dune',
        author: 'Frank Herbert',
        isbn: '9780441172719',
        publishedYear: 1965,
        coverUrl: null,
        alreadyInStore: false,
      },
      genre: 'SciFi',
      price: 18.99,
      stock: 3,
      description: '   ',
    });

    const request = http.expectOne({ method: 'POST', url: '/api/books' });
    expect(request.request.body).toEqual({
      openLibraryKey: '/works/OL893414W',
      title: 'Dune',
      author: 'Frank Herbert',
      isbn: '9780441172719',
      publishedYear: 1965,
      coverUrl: null,
      genre: 'SciFi',
      price: 18.99,
      stock: 3,
      description: null,
    });
    request.flush(duneDto);
    expect((await result).id).toBe('b1');
  });

  it('turns a 404 into a BookNotFoundError', async () => {
    const result = repository.remove('b1');
    http
      .expectOne('/api/books/b1')
      .flush({ title: 'Not found' }, { status: 404, statusText: 'Not Found' });

    await expect(result).rejects.toBeInstanceOf(BookNotFoundError);
  });

  it('turns a 400 ProblemDetails into a ValidationError with field errors', async () => {
    const result = repository.update('b1', {
      genre: 'SciFi',
      price: -1,
      stock: 0,
      description: null,
    });
    http
      .expectOne('/api/books/b1')
      .flush(
        { title: 'Validation failed', errors: { price: ['Price must be at least 0.'] } },
        { status: 400, statusText: 'Bad Request' },
      );

    const error = await result.catch((e: unknown) => e);
    expect(error).toBeInstanceOf(ValidationError);
    expect((error as ValidationError).fieldErrors['price']).toEqual(['Price must be at least 0.']);
  });
});
