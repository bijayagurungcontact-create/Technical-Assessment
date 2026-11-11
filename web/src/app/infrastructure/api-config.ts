import { InjectionToken } from '@angular/core';

/**
 * Base URL of the Book Store API. Defaults to the relative "/api", which the dev server
 * proxies to the .NET API (see proxy.conf.json), so the browser never needs CORS in dev.
 */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => '/api',
});
