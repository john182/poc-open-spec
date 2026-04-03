import { Component, inject, signal, computed, OnInit, OnDestroy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { CheckboxModule } from 'primeng/checkbox';
import { MultiSelectModule } from 'primeng/multiselect';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { MenuItem } from 'primeng/api';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { ErrorStateComponent } from '../../../../shared/components/error-state/error-state.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { CrawlerService } from '../services/crawler.service';
import { StatusCrawler, ProgressoUf } from '../models/crawler.models';

const SIGLAS_UF = [
  'AC', 'AL', 'AM', 'AP', 'BA', 'CE', 'DF', 'ES', 'GO',
  'MA', 'MG', 'MS', 'MT', 'PA', 'PB', 'PE', 'PI', 'PR',
  'RJ', 'RN', 'RO', 'RR', 'RS', 'SC', 'SE', 'SP', 'TO',
];

const INTERVALO_POLLING_MS = 5000;

const FASES_CRAWLER = [
  'DescobertaConvenios',
  'Sondagem',
  'ProcessamentoFila',
] as const;

const LABELS_FASES_CRAWLER: Record<string, string> = {
  DescobertaConvenios: 'Descoberta de Convênios',
  Sondagem: 'Sondagem',
  ProcessamentoFila: 'Processamento da Fila',
};

@Component({
  selector: 'app-crawler-status',
  standalone: true,
  imports: [
    FormsModule, DatePipe, ButtonModule, InputTextModule, CheckboxModule,
    MultiSelectModule, TagModule, TooltipModule,
    PageHeaderComponent, LoadingSpinnerComponent, ErrorStateComponent, EmptyStateComponent,
  ],
  templateUrl: './crawler-status.component.html',
  styleUrl: './crawler-status.component.scss',
})
export class CrawlerStatusComponent implements OnInit, OnDestroy {
  private readonly _crawlerService = inject(CrawlerService);
  private _intervalPolling: ReturnType<typeof setInterval> | null = null;

  readonly migalhas: MenuItem[] = [
    { label: 'Administração', routerLink: '/admin' },
    { label: 'Crawler', routerLink: '/admin/crawler' },
    { label: 'Status' },
  ];

  readonly statusAtual = signal<StatusCrawler | null>(null);
  readonly carregando = signal(true);
  readonly erro = signal('');

  readonly opcoesUf = SIGLAS_UF.map(uf => ({ label: uf, value: uf }));
  readonly executando = signal(false);
  readonly executandoCapitais = signal(false);
  readonly mensagemExecucao = signal('');
  readonly erroExecucao = signal('');
  readonly forcarReprocessamento = signal(false);
  readonly filtroUfs = signal<string[]>([]);

  readonly listaProgressoUfs = signal<ProgressoUf[]>([]);

  readonly ufsConcluidasLista = computed(() => {
    return this.listaProgressoUfs()
      .filter(p => p.status === 'Concluido')
      .map(p => p.uf);
  });

  ngOnInit(): void {
    this._carregarStatus();
  }

  ngOnDestroy(): void {
    this._pararPolling();
  }

  tentarNovamente(): void {
    this._carregarStatus();
  }

  executarCrawler(): void {
    this.executando.set(true);
    this.mensagemExecucao.set('');
    this.erroExecucao.set('');

    const ufsSelecionadas = this.filtroUfs();
    const ufs = ufsSelecionadas.length > 0 ? ufsSelecionadas : undefined;

    this._crawlerService.executar({
      forcarReprocessamento: this.forcarReprocessamento(),
      ufs,
    }).subscribe({
      next: (resposta) => {
        this.mensagemExecucao.set(resposta.mensagem);
        this.executando.set(false);
        this._carregarStatus();
      },
      error: (err) => {
        const msg = err.error?.erro ?? 'Erro ao iniciar execução do crawler.';
        this.erroExecucao.set(msg);
        this.executando.set(false);
      },
    });
  }

  executarCapitaisPrimeiro(): void {
    this.executandoCapitais.set(true);
    this.mensagemExecucao.set('');
    this.erroExecucao.set('');

    this._crawlerService.executar({
      forcarReprocessamento: this.forcarReprocessamento(),
      capitaisPrimeiro: true,
    }).subscribe({
      next: (resposta) => {
        this.mensagemExecucao.set(resposta.mensagem);
        this.executandoCapitais.set(false);
        this._carregarStatus();
      },
      error: (err) => {
        const msg = err.error?.erro ?? 'Erro ao iniciar execução do crawler.';
        this.erroExecucao.set(msg);
        this.executandoCapitais.set(false);
      },
    });
  }

  obterSeveridadeStatus(status: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    switch (status?.toLowerCase()) {
      case 'concluido': return 'success';
      case 'emandamento': return 'info';
      case 'falhaparcial': return 'warn';
      case 'falha': return 'danger';
      case 'nenhumaexecucao': return 'secondary';
      default: return 'secondary';
    }
  }

  obterSeveridadeProgressoUf(status: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    switch (status?.toLowerCase()) {
      case 'concluido': return 'success';
      case 'emandamento': return 'info';
      case 'falha': return 'danger';
      case 'interrompido': return 'warn';
      default: return 'secondary';
    }
  }

  readonly fasesCrawler = FASES_CRAWLER;
  readonly labelsFasesCrawler = LABELS_FASES_CRAWLER;

  obterIndiceFaseAtual(): number {
    const faseAtual = this.statusAtual()?.faseAtual;
    if (faseAtual === 'Concluido') return FASES_CRAWLER.length;
    const indice = FASES_CRAWLER.indexOf(faseAtual as typeof FASES_CRAWLER[number]);
    return indice >= 0 ? indice : -1;
  }

  obterSeveridadeFase(fase: string): 'success' | 'info' | 'secondary' {
    const indiceFaseAtual = this.obterIndiceFaseAtual();
    const indiceFase = FASES_CRAWLER.indexOf(fase as typeof FASES_CRAWLER[number]);

    if (indiceFase < indiceFaseAtual) return 'success';
    if (indiceFase === indiceFaseAtual) return 'info';
    return 'secondary';
  }

  obterIconeFase(fase: string): string {
    const indiceFaseAtual = this.obterIndiceFaseAtual();
    const indiceFase = FASES_CRAWLER.indexOf(fase as typeof FASES_CRAWLER[number]);

    if (indiceFase < indiceFaseAtual) return 'pi pi-check';
    if (indiceFase === indiceFaseAtual) return 'pi pi-spin pi-spinner';
    return 'pi pi-circle';
  }

  obterLabelProgressoUf(status: string): string {
    if (status === 'Concluido') return 'Convênios verificados';
    if (status === 'EmAndamento') return 'Em andamento';
    return status;
  }

  private _carregarStatus(): void {
    this.carregando.set(true);
    this.erro.set('');
    this._crawlerService.obterStatus().subscribe({
      next: (status) => {
        this.statusAtual.set(status);
        this._atualizarListaProgressoUfs(status);
        this.carregando.set(false);
        this._gerenciarPolling(status);
      },
      error: () => {
        this.erro.set('Erro ao carregar status do crawler.');
        this.carregando.set(false);
        this._pararPolling();
      },
    });
  }

  private _atualizarListaProgressoUfs(status: StatusCrawler): void {
    if (status.progressoUfs && Object.keys(status.progressoUfs).length > 0) {
      this.listaProgressoUfs.set(Object.values(status.progressoUfs));
    } else {
      this.listaProgressoUfs.set([]);
    }
  }

  private _gerenciarPolling(status: StatusCrawler): void {
    if (status.status === 'EmAndamento') {
      this._iniciarPolling();
    } else {
      this._pararPolling();
    }
  }

  private _iniciarPolling(): void {
    if (this._intervalPolling) return;
    this._intervalPolling = setInterval(() => {
      this._crawlerService.obterStatus().subscribe({
        next: (status) => {
          this.statusAtual.set(status);
          this._atualizarListaProgressoUfs(status);
          this._gerenciarPolling(status);
        },
        error: () => {
          this._pararPolling();
        },
      });
    }, INTERVALO_POLLING_MS);
  }

  private _pararPolling(): void {
    if (this._intervalPolling) {
      clearInterval(this._intervalPolling);
      this._intervalPolling = null;
    }
  }
}
