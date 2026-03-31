import { Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';

export interface FiltroConfig {
  chave: string;
  rotulo: string;
  tipo: 'text' | 'number';
  placeholder?: string;
}

@Component({
  selector: 'app-filter-bar',
  standalone: true,
  imports: [FormsModule, InputTextModule, ButtonModule],
  template: `
    <div class="filter-bar" data-cy="filter-bar">
      @for (filtro of filtros(); track filtro.chave) {
        <div class="filter-item">
          <label>{{ filtro.rotulo }}</label>
          <input
            pInputText
            [type]="filtro.tipo"
            [placeholder]="filtro.placeholder || ''"
            [ngModel]="valores[filtro.chave] || ''"
            (ngModelChange)="onValorMudou(filtro.chave, $event)"
          />
        </div>
      }
      <p-button label="Limpar" icon="pi pi-times" severity="secondary" [text]="true" (onClick)="onLimpar()" />
    </div>
  `,
  styles: [`
    .filter-bar { display: flex; flex-wrap: wrap; gap: 1rem; align-items: flex-end; margin-bottom: 1rem; }
    .filter-item { display: flex; flex-direction: column; gap: 0.25rem; }
    .filter-item label { font-size: 0.75rem; font-weight: 600; color: var(--color-text-muted); }
  `],
})
export class FilterBarComponent {
  filtros = input.required<FiltroConfig[]>();
  filtroMudou = output<Record<string, string>>();
  limpar = output<void>();
  valores: Record<string, string> = {};

  onValorMudou(chave: string, valor: string): void {
    this.valores[chave] = valor;
    this.filtroMudou.emit({ ...this.valores });
  }

  onLimpar(): void {
    this.valores = {};
    this.limpar.emit();
  }
}
