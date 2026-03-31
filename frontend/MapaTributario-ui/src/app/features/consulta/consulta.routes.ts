import { Routes } from '@angular/router';

export const CONSULTA_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./consulta-placeholder.component').then(m => m.ConsultaPlaceholderComponent) },
];
