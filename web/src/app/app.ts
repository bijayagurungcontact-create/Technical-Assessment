import { Component } from '@angular/core';
import { Body } from './presentation/layout/body/body';
import { Footer } from './presentation/layout/footer/footer';
import { Header } from './presentation/layout/header/header';

/**
 * Application shell: exactly three sibling components. They share state through the
 * application layer (BookListStore), never through each other.
 */
@Component({
  selector: 'app-root',
  imports: [Header, Body, Footer],
  templateUrl: './app.html',
  host: { class: 'flex min-h-screen flex-col' },
})
export class App {}
