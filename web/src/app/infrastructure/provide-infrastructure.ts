import { provideHttpClient, withFetch } from '@angular/common/http';
import { EnvironmentProviders, makeEnvironmentProviders } from '@angular/core';
import { BookRepository } from '../domain/book.repository';
import { CatalogRepository } from '../domain/catalog.repository';
import { BookHttpRepository } from './book-http.repository';
import { CatalogHttpRepository } from './catalog-http.repository';

/** Binds the domain ports to their HTTP adapters. The only place that wiring happens. */
export function provideInfrastructure(): EnvironmentProviders {
  return makeEnvironmentProviders([
    provideHttpClient(withFetch()),
    { provide: BookRepository, useClass: BookHttpRepository },
    { provide: CatalogRepository, useClass: CatalogHttpRepository },
  ]);
}
