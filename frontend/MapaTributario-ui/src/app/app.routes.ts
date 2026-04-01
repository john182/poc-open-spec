import { Routes } from '@angular/router';
import { adminGuard } from './core/guards/admin.guard';
import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from './core/guards/guest.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./layout/components/app-layout.component').then((m) => m.AppLayoutComponent),
    children: [
      { path: '', redirectTo: 'consulta', pathMatch: 'full' },
      {
        path: 'consulta',
        canActivate: [authGuard],
        canActivateChild: [authGuard],
        loadChildren: () =>
          import('./features/consulta/consulta.routes').then((m) => m.CONSULTA_ROUTES),
      },
      {
        path: 'admin',
        canActivate: [adminGuard],
        canActivateChild: [adminGuard],
        loadChildren: () =>
          import('./features/admin/admin.routes').then((m) => m.ADMIN_ROUTES),
      },
    ],
  },
  {
    path: 'auth',
    canActivate: [guestGuard],
    canActivateChild: [guestGuard],
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
