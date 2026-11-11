import { HttpErrorResponse } from '@angular/common/http';
import {
  CatalogUnavailableError,
  DuplicateBookError,
  ServerUnreachableError,
  UnexpectedError,
} from '../domain/errors';
import { toDomainError } from './http-error.mapper';

const httpError = (status: number, error: unknown = null) =>
  new HttpErrorResponse({ status, error });

describe('toDomainError', () => {
  it('maps 409 to a DuplicateBookError carrying the API detail', () => {
    const error = toDomainError(httpError(409, { detail: "'Dune' is already in the store." }));
    expect(error).toBeInstanceOf(DuplicateBookError);
    expect(error.message).toBe("'Dune' is already in the store.");
  });

  it('maps 503 (Open Library down) to a CatalogUnavailableError', () => {
    expect(toDomainError(httpError(503, { detail: 'Open Library is down' }))).toBeInstanceOf(
      CatalogUnavailableError,
    );
  });

  it('maps a network failure (status 0) to ServerUnreachableError', () => {
    expect(toDomainError(httpError(0))).toBeInstanceOf(ServerUnreachableError);
  });

  it('never leaks raw server errors for unexpected statuses', () => {
    const error = toDomainError(httpError(500, { detail: 'NullReferenceException at ...' }));
    expect(error).toBeInstanceOf(UnexpectedError);
    expect(error.message).not.toContain('NullReference');
  });
});
