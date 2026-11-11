import { HttpErrorResponse } from '@angular/common/http';
import {
  BookNotFoundError,
  BookStoreError,
  CatalogUnavailableError,
  DuplicateBookError,
  ServerUnreachableError,
  UnexpectedError,
  ValidationError,
} from '../domain/errors';
import { ProblemDetailsDto } from './api.dtos';

/** Translates an HTTP failure (with its ProblemDetails body) into a domain error. */
export function toDomainError(error: unknown): BookStoreError {
  if (error instanceof BookStoreError) {
    return error;
  }
  if (!(error instanceof HttpErrorResponse)) {
    return new UnexpectedError();
  }

  const problem = (
    typeof error.error === 'object' ? error.error : null
  ) as ProblemDetailsDto | null;
  const detail = problem?.detail;

  switch (error.status) {
    case 0:
      return new ServerUnreachableError();
    case 400:
      return new ValidationError(
        detail ?? 'Please correct the highlighted fields.',
        problem?.errors ?? {},
      );
    case 404:
      return new BookNotFoundError();
    case 409:
      return new DuplicateBookError(detail ?? 'This book is already in the store.');
    case 503:
    case 504:
      return new CatalogUnavailableError(detail);
    default:
      return new UnexpectedError();
  }
}
