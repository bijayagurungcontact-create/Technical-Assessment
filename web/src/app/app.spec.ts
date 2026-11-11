import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { App } from './app';

@Component({ selector: 'app-header', template: '' })
class HeaderStub {}

@Component({ selector: 'app-body', template: '' })
class BodyStub {}

@Component({ selector: 'app-footer', template: '' })
class FooterStub {}

describe('App shell', () => {
  it('renders exactly Header, Body and Footer as direct siblings', () => {
    TestBed.overrideComponent(App, { set: { imports: [HeaderStub, BodyStub, FooterStub] } });
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const children = Array.from((fixture.nativeElement as HTMLElement).children);
    expect(children.map((el) => el.tagName.toLowerCase())).toEqual([
      'app-header',
      'app-body',
      'app-footer',
    ]);
  });
});
