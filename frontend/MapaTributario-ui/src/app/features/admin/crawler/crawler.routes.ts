import { Routes } from '@angular/router';

export const CRAWLER_ROUTES: Routes = [
  {
    path: '',
    redirectTo: 'status',
    pathMatch: 'full',
  },
  {
    path: 'status',
    loadComponent: () =>
      import('./status/crawler-status.component').then((m) => m.CrawlerStatusComponent),
  },
  {
    path: 'certificado',
    loadComponent: () =>
      import('./certificado/crawler-certificado.component').then(
        (m) => m.CrawlerCertificadoComponent,
      ),
  },
  {
    path: 'execucoes',
    loadComponent: () =>
      import('./execucoes/crawler-execucoes.component').then(
        (m) => m.CrawlerExecucoesComponent,
      ),
  },
  {
    path: 'configuracao',
    loadComponent: () =>
      import('./configuracao/crawler-configuracao.component').then(
        (m) => m.CrawlerConfiguracaoComponent,
      ),
  },
];
