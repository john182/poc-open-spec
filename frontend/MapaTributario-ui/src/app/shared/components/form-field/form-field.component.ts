import { Component, input } from '@angular/core';
import { AbstractControl, ReactiveFormsModule } from '@angular/forms';
import { MENSAGENS_VALIDACAO } from '../../utils/mensagens-validacao';

@Component({
  selector: 'app-form-field',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <div class="form-field">
      <label>{{ rotulo() }}</label>
      <ng-content />
      @if (controle()?.invalid && controle()?.dirty) {
        @for (erro of errosAtivos(); track erro) {
          <small class="erro-mensagem">{{ erro }}</small>
        }
      }
    </div>
  `,
  styles: [`
    .form-field { display: flex; flex-direction: column; gap: 0.25rem; margin-bottom: 1rem; }
    label { font-size: 0.875rem; font-weight: 500; color: var(--color-text-primary); }
    .erro-mensagem { color: var(--color-danger); font-size: 0.75rem; }
  `],
})
export class FormFieldComponent {
  rotulo = input.required<string>();
  controle = input.required<AbstractControl | null>();
  mensagensErro = input<Record<string, string>>({});

  errosAtivos(): string[] {
    const ctrl = this.controle();
    if (!ctrl?.errors) return [];
    const mensagens = { ...MENSAGENS_VALIDACAO, ...this.mensagensErro() };
    return Object.keys(ctrl.errors)
      .filter((key) => ctrl.errors![key])
      .map((key) => mensagens[key] || `Erro: ${key}`);
  }
}
