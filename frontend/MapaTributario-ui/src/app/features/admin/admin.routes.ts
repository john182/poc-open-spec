import { Routes } from '@angular/router';

export const ADMIN_ROUTES: Routes = [
  {
    path: 'crawler',
    loadChildren: () =>
      import('./crawler/crawler.routes').then((m) => m.CRAWLER_ROUTES),
  },
];
