import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./layout/components/app-layout.component').then((m) => m.AppLayoutComponent),
    children: [
      { path: '', redirectTo: 'consulta', pathMatch: 'full' },
      {
        path: 'consulta',
        loadChildren: () =>
          import('./features/consulta/consulta.routes').then((m) => m.CONSULTA_ROUTES),
      },
    ],
  },
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then((m) => m.AUTH_ROUTES),
  },
  {
    path: 'acesso-negado',
    loadComponent: () =>
      import('./features/errors/access-denied.component').then((m) => m.AccessDeniedComponent),
  },
  {
    path: '**',
    loadComponent: () =>
      import('./features/errors/not-found.component').then((m) => m.NotFoundComponent),
  },
];
