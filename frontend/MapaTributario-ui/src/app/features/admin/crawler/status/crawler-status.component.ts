import { Component, inject, signal, OnInit } from '@angular/core';
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
import { StatusCrawler } from '../models/crawler.models';

const SIGLAS_UF = [
  'AC', 'AL', 'AM', 'AP', 'BA', 'CE', 'DF', 'ES', 'GO',
  'MA', 'MG', 'MS', 'MT', 'PA', 'PB', 'PE', 'PI', 'PR',
  'RJ', 'RN', 'RO', 'RR', 'RS', 'SC', 'SE', 'SP', 'TO',
];

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
export class CrawlerStatusComponent implements OnInit {
  private readonly _crawlerService = inject(CrawlerService);

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
  readonly mensagemExecucao = signal('');
  readonly erroExecucao = signal('');
  readonly forcarReprocessamento = signal(false);
  readonly filtroUfs = signal<string[]>([]);

  ngOnInit(): void {
    this._carregarStatus();
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

  private _carregarStatus(): void {
    this.carregando.set(true);
    this.erro.set('');
    this._crawlerService.obterStatus().subscribe({
      next: (status) => {
        // NenhumaExecucao agora vem como 200 com status "NenhumaExecucao"
        if (status.status === 'NenhumaExecucao') {
          this.statusAtual.set(status);
        } else {
          this.statusAtual.set(status);
        }
        this.carregando.set(false);
      },
      error: () => {
        this.erro.set('Erro ao carregar status do crawler.');
        this.carregando.set(false);
      },
    });
  }
}
