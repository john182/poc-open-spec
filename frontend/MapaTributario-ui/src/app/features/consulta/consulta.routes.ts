import { Routes } from '@angular/router';

export const CONSULTA_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./mapa/consulta-mapa.component').then(m => m.ConsultaMapaComponent),
  },
  {
    path: 'estado/:uf',
    loadComponent: () =>
      import('./estado/estado-municipios.component').then(m => m.EstadoMunicipiosComponent),
  },
  {
    path: 'municipio/:codigoIbge',
    loadComponent: () =>
      import('./municipio/municipio-aliquotas.component').then(m => m.MunicipioAliquotasComponent),
  },
];
