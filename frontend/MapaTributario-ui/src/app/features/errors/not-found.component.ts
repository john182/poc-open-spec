import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [RouterLink, ButtonModule],
  template: `
    <div class="error-page" data-cy="not-found-page">
      <h1 class="error-code">404</h1>
      <p>Página não encontrada</p>
      <a routerLink="/"><p-button label="Voltar ao início" icon="pi pi-home" /></a>
    </div>
  `,
  styles: [`
    .error-page { display: flex; flex-direction: column; align-items: center; justify-content: center; min-height: 80vh; text-align: center; }
    .error-code { font-size: 6rem; font-weight: 800; color: var(--color-primary); margin: 0; }
    p { font-size: 1.25rem; color: var(--color-text-secondary); margin: 0.5rem 0 2rem; }
  `],
})
export class NotFoundComponent {}
