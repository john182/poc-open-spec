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
  templateUrl: './filter-bar.component.html',
  styleUrl: './filter-bar.component.scss',
})
export class FilterBarComponent {
  filtros = input.required<FiltroConfig[]>();
  filtroMudou = output<Record<string, string | number>>();
  limpar = output<void>();
  valores: Record<string, string | number> = {};

  onValorMudou(chave: string, valor: string | number): void {
    const filtro = this.filtros().find((f) => f.chave === chave);
    if (filtro?.tipo === 'number' && valor !== '') {
      const numero = typeof valor === 'number' ? valor : Number(valor);
      this.valores[chave] = Number.isNaN(numero) ? valor : numero;
    } else {
      this.valores[chave] = valor;
    }
    this.filtroMudou.emit({ ...this.valores });
  }

  onLimpar(): void {
    this.valores = {};
    this.limpar.emit();
  }
}
