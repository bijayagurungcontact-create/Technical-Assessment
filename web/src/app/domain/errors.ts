/**
 * Failures the rest of the app can reason about without knowing they came over HTTP.
 * The infrastructure layer translates transport errors into these.
 */
export abstract class BookStoreError extends Error {
  protected constructor(message: string) {
    super(message);
    this.name = new.target.name;
  }
}

/** Input was rejected. `fieldErrors` is keyed by field name (e.g. "price"). */
export class ValidationError extends BookStoreError {
  constructor(
    message: string,
    readonly fieldErrors: Readonly<Record<string, readonly string[]>>,
  ) {
    super(message);
  }
}

export class DuplicateBookError extends BookStoreError {
  constructor(message: string) {
    super(message);
  }
}

export class BookNotFoundError extends BookStoreError {
  constructor(message = 'This book no longer exists. It may have been deleted.') {
    super(message);
  }
}

/** Open Library (behind our API) is slow or down. */
export class CatalogUnavailableError extends BookStoreError {
  constructor(message = 'Open Library is currently unavailable. Please try again shortly.') {
    super(message);
  }
}

/** Our own API could not be reached at all. */
export class ServerUnreachableError extends BookStoreError {
  constructor(message = 'Cannot reach the Book Store server. Is the API running?') {
    super(message);
  }
}

export class UnexpectedError extends BookStoreError {
  constructor(message = 'Something went wrong. Please try again.') {
    super(message);
  }
}

export function errorMessage(error: unknown): string {
  return error instanceof BookStoreError ? error.message : new UnexpectedError().message;
}
