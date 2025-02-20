import { Routes } from '@angular/router';
import { MarkdownViewerComponent } from './markdown-viewer/markdown-viewer.component';

export const routes: Routes = [  
  { path: 'doc/:docPath', component: MarkdownViewerComponent },
  { path: '', redirectTo: '/doc/overview', pathMatch: 'full' },
  { path: '**', redirectTo: 'doc/overview', pathMatch: 'full' } // Wildcard fallback
];
