import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-access-denied',
  standalone: true,
  imports: [RouterLink, ButtonModule],
  template: `
    <div class="error-page" data-cy="access-denied-page">
      <i class="pi pi-lock error-page-icon"></i>
      <h1>Acesso Negado</h1>
      <p>Você não tem permissão para acessar esta página.</p>
      <a routerLink="/"><p-button label="Voltar ao início" icon="pi pi-home" /></a>
    </div>
  `,
  styles: [`
    .error-page { display: flex; flex-direction: column; align-items: center; justify-content: center; min-height: 80vh; text-align: center; }
    .error-page-icon { font-size: 4rem; color: var(--color-warning); margin-bottom: 1rem; }
    h1 { margin: 0 0 0.5rem; color: var(--color-text-primary); }
    p { color: var(--color-text-secondary); margin: 0 0 2rem; }
  `],
})
export class AccessDeniedComponent {}
