import { Component, input, output } from '@angular/core';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-error-state',
  standalone: true,
  imports: [ButtonModule],
  template: `
    <div class="error-container" data-cy="error-state">
      <i class="pi pi-exclamation-triangle error-icon"></i>
      <h3>{{ titulo() }}</h3>
      <p>{{ mensagem() }}</p>
      <p-button [label]="tentarNovamenteLabel()" (onClick)="tentarNovamente.emit()" icon="pi pi-refresh" />
    </div>
  `,
  styles: [`
    .error-container { display: flex; flex-direction: column; align-items: center; padding: 3rem; text-align: center; }
    .error-icon { font-size: 3rem; color: var(--color-danger); margin-bottom: 1rem; }
    h3 { margin: 0 0 0.5rem; color: var(--color-text-primary); }
    p { color: var(--color-text-secondary); margin: 0 0 1.5rem; }
  `],
})
export class ErrorStateComponent {
  titulo = input<string>('Erro ao carregar dados');
  mensagem = input.required<string>();
  tentarNovamenteLabel = input<string>('Tentar novamente');
  tentarNovamente = output<void>();
}
