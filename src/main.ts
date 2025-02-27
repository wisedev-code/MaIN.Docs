// src/main.ts
import { bootstrapApplication } from '@angular/platform-browser';
import { importProvidersFrom } from '@angular/core';
import { provideRouter, withHashLocation } from '@angular/router';
import { HttpClientModule, HttpClient } from '@angular/common/http';
import { MarkdownModule } from 'ngx-markdown';

import { AppComponent } from './app/app.component';
import { routes } from './app/app.routes';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

bootstrapApplication(AppComponent, {
  providers: [
    provideRouter(routes, withHashLocation()),
    importProvidersFrom(
      HttpClientModule,
      MarkdownModule.forRoot({ loader: HttpClient })
    ), provideAnimationsAsync()
  ]
})
.catch(err => console.error(err));
