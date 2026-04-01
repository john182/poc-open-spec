import { Component, input } from '@angular/core';
import { AbstractControl } from '@angular/forms';
import { MENSAGENS_VALIDACAO } from '../../utils/mensagens-validacao';

@Component({
  selector: 'app-form-field',
  standalone: true,
  templateUrl: './form-field.component.html',
  styleUrl: './form-field.component.scss',
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
      .map((key) => mensagens[key] || 'Erro: ' + key);
  }
}
