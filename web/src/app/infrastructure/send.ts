import { firstValueFrom, Observable } from 'rxjs';
import { toDomainError } from './http-error.mapper';

/** Awaits a single-response HTTP call, translating any failure into a domain error. */
export async function send<T>(request: Observable<T>): Promise<T> {
  try {
    return await firstValueFrom(request);
  } catch (error) {
    throw toDomainError(error);
  }
}
