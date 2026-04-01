import { Component, input, output } from '@angular/core';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-error-state',
  standalone: true,
  imports: [ButtonModule],
  templateUrl: './error-state.component.html',
  styleUrl: './error-state.component.scss',
})
export class ErrorStateComponent {
  titulo = input<string>('Erro ao carregar dados');
  mensagem = input.required<string>();
  tentarNovamenteLabel = input<string>('Tentar novamente');
  tentarNovamente = output<void>();
}
