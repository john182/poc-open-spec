import { Component, inject, signal, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ButtonModule } from 'primeng/button';
import { MenuItem } from 'primeng/api';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../../../shared/components/error-state/error-state.component';
import { CrawlerService } from '../services/crawler.service';
import { StatusCrawler } from '../models/crawler.models';

@Component({
  selector: 'app-crawler-execucoes',
  standalone: true,
  imports: [
    DatePipe, TableModule, TagModule, ButtonModule,
    PageHeaderComponent, LoadingSpinnerComponent, EmptyStateComponent, ErrorStateComponent,
  ],
  templateUrl: './crawler-execucoes.component.html',
  styleUrl: './crawler-execucoes.component.scss',
})
export class CrawlerExecucoesComponent implements OnInit {
  private readonly _crawlerService = inject(CrawlerService);

  readonly migalhas: MenuItem[] = [
    { label: 'Administração' },
    { label: 'Crawler' },
    { label: 'Histórico' },
  ];

  readonly execucoes = signal<StatusCrawler[]>([]);
  readonly carregando = signal(true);
  readonly erro = signal('');

  ngOnInit(): void {
    this._carregarExecucoes();
  }

  tentarNovamente(): void {
    this._carregarExecucoes();
  }

  obterSeveridadeStatus(status: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    switch (status?.toLowerCase()) {
      case 'concluido': return 'success';
      case 'executando': return 'info';
      case 'erro':
      case 'falha': return 'danger';
      case 'cancelado': return 'warn';
      default: return 'secondary';
    }
  }

  private _carregarExecucoes(): void {
    this.carregando.set(true);
    this.erro.set('');
    this._crawlerService.listarExecucoes().subscribe({
      next: (execucoes) => {
        this.execucoes.set(execucoes);
        this.carregando.set(false);
      },
      error: () => {
        this.erro.set('Erro ao carregar histórico de execuções.');
        this.carregando.set(false);
      },
    });
  }
}
