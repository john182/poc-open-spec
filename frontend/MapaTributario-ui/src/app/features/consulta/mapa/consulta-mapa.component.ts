import { Component, inject, signal, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { BrazilMapComponent } from './brazil-map.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { ErrorStateComponent } from '../../../shared/components/error-state/error-state.component';
import { ConsultaService } from '../services/consulta.service';
import { Estado } from '../models/consulta.models';

@Component({
  selector: 'app-consulta-mapa',
  standalone: true,
  imports: [BrazilMapComponent, PageHeaderComponent, LoadingSpinnerComponent, ErrorStateComponent],
  templateUrl: './consulta-mapa.component.html',
  styleUrl: './consulta-mapa.component.scss',
})
export class ConsultaMapaComponent implements OnInit {
  private readonly _consultaService = inject(ConsultaService);
  private readonly _router = inject(Router);

  readonly estados = signal<Estado[]>([]);
  readonly carregando = signal(true);
  readonly erro = signal('');
  readonly ufSelecionada = signal<string | null>(null);

  ngOnInit(): void {
    this._carregarEstados();
  }

  onEstadoSelecionado(uf: string): void {
    this.ufSelecionada.set(uf);
    this._router.navigate(['/consulta/estado', uf]);
  }

  tentarNovamente(): void {
    this._carregarEstados();
  }

  private _carregarEstados(): void {
    this.carregando.set(true);
    this.erro.set('');
    this._consultaService.listarEstados().subscribe({
      next: (estados) => {
        this.estados.set(estados);
        this.carregando.set(false);
      },
      error: () => {
        this.erro.set('Erro ao carregar estados. Tente novamente.');
        this.carregando.set(false);
      },
    });
  }
}
