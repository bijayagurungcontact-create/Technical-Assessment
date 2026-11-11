import { Component, input, signal } from '@angular/core';

/** Cover thumbnail with a neutral placeholder when there is no cover or it fails to load. */
@Component({
  selector: 'app-book-cover',
  template: `
    @if (url() && !failed()) {
      <img
        [src]="url()"
        [alt]="'Cover of ' + title()"
        loading="lazy"
        (error)="failed.set(true)"
        class="h-16 w-11 rounded object-cover shadow-sm"
      />
    } @else {
      <div
        class="flex h-16 w-11 items-center justify-center rounded bg-surface-100 text-surface-400"
        [attr.aria-label]="'No cover for ' + title()"
      >
        <i class="pi pi-book"></i>
      </div>
    }
  `,
})
export class BookCover {
  readonly url = input<string | null>(null);
  readonly title = input('');
  protected readonly failed = signal(false);
}
