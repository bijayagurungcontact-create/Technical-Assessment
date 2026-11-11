import { Component } from '@angular/core';

@Component({
  selector: 'app-footer',
  template: `
    <footer class="border-t border-surface-200 bg-surface-0">
      <div
        class="mx-auto flex max-w-7xl flex-col gap-1 px-4 py-4 text-sm text-muted-color sm:flex-row sm:justify-between"
      >
        <span>Book Store &middot; technical assessment</span>
        <span>
          Catalog search powered by
          <a
            href="https://openlibrary.org"
            target="_blank"
            rel="noopener noreferrer"
            class="font-medium text-primary hover:underline"
            >Open Library</a
          >
        </span>
      </div>
    </footer>
  `,
})
export class Footer {}
