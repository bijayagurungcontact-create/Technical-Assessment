import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { providePrimeNG } from 'primeng/config';
import { provideInfrastructure } from './infrastructure/provide-infrastructure';
import { BookStorePreset } from './presentation/theme';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideInfrastructure(),
    providePrimeNG({
      theme: {
        preset: BookStorePreset,
        options: {
          darkModeSelector: false,
          // Must match the @layer order declared in styles.css.
          cssLayer: { name: 'primeng', order: 'theme, base, primeng' },
        },
      },
    }),
  ],
};
