import { Component, input } from '@angular/core';
import { ProgressSpinnerModule } from 'primeng/progressspinner';

@Component({
  selector: 'app-loading-spinner',
  standalone: true,
  imports: [ProgressSpinnerModule],
  template: `
    <div class="loading-container" data-cy="loading-spinner">
      <p-progressSpinner strokeWidth="4" />
      @if (mensagem()) {
        <p class="loading-message">{{ mensagem() }}</p>
      }
    </div>
  `,
  styles: [`
    .loading-container { display: flex; flex-direction: column; align-items: center; padding: 2rem; gap: 1rem; }
    .loading-message { color: var(--color-text-secondary); font-size: 0.875rem; }
  `],
})
export class LoadingSpinnerComponent {
  mensagem = input<string>();
}
