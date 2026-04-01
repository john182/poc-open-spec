import { Component, inject, signal, OnInit, OnDestroy, computed } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { IconField } from 'primeng/iconfield';
import { InputIcon } from 'primeng/inputicon';
import { MenuItem } from 'primeng/api';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../../shared/components/error-state/error-state.component';
import { ConsultaService } from '../services/consulta.service';
import { Municipio, StatusProcessamentoUf } from '../models/consulta.models';

const POLLING_INTERVALO_MS = 10_000;

const STATUS_COM_POLLING: StatusProcessamentoUf[] = [
  'processamentoIniciado',
  'processando',
  'atualizando',
  'aguardandoProcessamento',
];

@Component({
  selector: 'app-estado-municipios',
  standalone: true,
  imports: [
    FormsModule, InputTextModule, IconField, InputIcon, PageHeaderComponent,
    LoadingSpinnerComponent, EmptyStateComponent, ErrorStateComponent,
  ],
  templateUrl: './estado-municipios.component.html',
  styleUrl: './estado-municipios.component.scss',
})
export class EstadoMunicipiosComponent implements OnInit, OnDestroy {
  private readonly _route = inject(ActivatedRoute);
  private readonly _router = inject(Router);
  private readonly _consultaService = inject(ConsultaService);
  private _pollingTimer: ReturnType<typeof setInterval> | null = null;

  readonly uf = signal('');
  readonly municipios = signal<Municipio[]>([]);
  readonly carregando = signal(true);
  readonly erro = signal('');
  readonly textoBusca = signal('');
  readonly statusProcessamento = signal<StatusProcessamentoUf | ''>('');
  readonly ultimoProcessamento = signal<string | null>(null);

  readonly migalhas = computed<MenuItem[]>(() => [
    { label: 'Consulta', routerLink: '/consulta' },
    { label: this.uf() },
  ]);

  readonly municipiosFiltrados = computed(() => {
    const texto = this.textoBusca().toLowerCase();
    if (!texto) return this.municipios();
    return this.municipios().filter(m => m.nome.toLowerCase().includes(texto));
  });

  readonly statusEmProcessamento = computed(() => {
    const status = this.statusProcessamento();
    return STATUS_COM_POLLING.includes(status as StatusProcessamentoUf);
  });

  readonly exibirListaMunicipios = computed(() => {
    const status = this.statusProcessamento();
    return status === 'concluido' || status === 'vencido' || status === 'atualizando'
      || (status === 'processando' && this.municipios().length > 0);
  });

  readonly exibindoDadosVencidos = computed(() => {
    const status = this.statusProcessamento();
    return (status === 'vencido' || status === 'atualizando') && this.municipios().length > 0;
  });

  readonly mensagemProcessamento = computed(() => {
    const status = this.statusProcessamento();
    switch (status) {
      case 'processamentoIniciado':
        return 'O processamento dos dados deste estado foi iniciado. Aguarde alguns instantes, os dados aparecerão automaticamente.';
      case 'processando':
        return this.municipios().length > 0
          ? 'O processamento ainda está em andamento. Os dados abaixo são parciais e serão atualizados automaticamente.'
          : 'Os dados deste estado estão sendo processados. Aguarde alguns instantes.';
      case 'aguardandoProcessamento':
        return 'Este estado está na fila de processamento. Aguarde alguns instantes, os dados aparecerão automaticamente.';
      case 'atualizando':
        return 'Os dados estão sendo atualizados. Os dados exibidos abaixo podem estar desatualizados.';
      case 'vencido':
        return 'Os dados exibidos podem estar desatualizados. O reprocessamento será iniciado na próxima consulta.';
      default:
        return '';
    }
  });

  ngOnInit(): void {
    const uf = this._route.snapshot.paramMap.get('uf') ?? '';
    this.uf.set(uf.toUpperCase());
    this._carregarMunicipios();
  }

  ngOnDestroy(): void {
    this._pararPolling();
  }

  onMunicipioClick(codigoIbge: string): void {
    const municipio = this.municipios().find(m => m.codigoIbge === codigoIbge);
    this._router.navigate(['/consulta/municipio', codigoIbge], {
      queryParams: { uf: this.uf(), nome: municipio?.nome },
    });
  }

  tentarNovamente(): void {
    this._carregarMunicipios();
  }

  private _carregarMunicipios(): void {
    this.carregando.set(true);
    this.erro.set('');
    this._consultaService.listarMunicipios(this.uf()).subscribe({
      next: (resposta) => {
        this.statusProcessamento.set(resposta.statusProcessamento);
        this.ultimoProcessamento.set(resposta.ultimoProcessamento);
        this.municipios.set(resposta.municipios);
        this.carregando.set(false);
        this._gerenciarPolling(resposta.statusProcessamento);
      },
      error: () => {
        this.erro.set('Erro ao carregar municípios. Tente novamente.');
        this.carregando.set(false);
        this._pararPolling();
      },
    });
  }

  private _atualizarDados(): void {
    this._consultaService.listarMunicipios(this.uf()).subscribe({
      next: (resposta) => {
        this.statusProcessamento.set(resposta.statusProcessamento);
        this.ultimoProcessamento.set(resposta.ultimoProcessamento);
        this.municipios.set(resposta.municipios);
        this._gerenciarPolling(resposta.statusProcessamento);
      },
      error: () => {
        // Erro silencioso no polling -- mantém dados atuais
      },
    });
  }

  private _gerenciarPolling(status: StatusProcessamentoUf): void {
    if (STATUS_COM_POLLING.includes(status)) {
      this._iniciarPolling();
    } else {
      this._pararPolling();
    }
  }

  private _iniciarPolling(): void {
    if (this._pollingTimer !== null) return;
    this._pollingTimer = setInterval(() => this._atualizarDados(), POLLING_INTERVALO_MS);
  }

  private _pararPolling(): void {
    if (this._pollingTimer !== null) {
      clearInterval(this._pollingTimer);
      this._pollingTimer = null;
    }
  }
}
