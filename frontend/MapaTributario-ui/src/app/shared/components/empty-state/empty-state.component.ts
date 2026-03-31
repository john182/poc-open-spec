import { Component, input, output } from '@angular/core';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [ButtonModule],
  template: `
    <div class="empty-container" data-cy="empty-state">
      <i [class]="icone() || 'pi pi-inbox'" class="empty-icon"></i>
      <h3>{{ titulo() }}</h3>
      <p>{{ mensagem() }}</p>
      @if (acaoLabel()) {
        <p-button [label]="acaoLabel()!" (onClick)="acao.emit()" severity="secondary" />
      }
    </div>
  `,
  styles: [`
    .empty-container { display: flex; flex-direction: column; align-items: center; padding: 3rem; text-align: center; }
    .empty-icon { font-size: 3rem; color: var(--color-text-muted); margin-bottom: 1rem; }
    h3 { margin: 0 0 0.5rem; color: var(--color-text-primary); }
    p { color: var(--color-text-secondary); margin: 0 0 1.5rem; }
  `],
})
export class EmptyStateComponent {
  titulo = input.required<string>();
  mensagem = input.required<string>();
  icone = input<string>();
  acaoLabel = input<string>();
  acao = output<void>();
}
