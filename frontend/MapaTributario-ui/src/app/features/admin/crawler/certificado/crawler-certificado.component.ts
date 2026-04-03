import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { FileUploadModule } from 'primeng/fileupload';
import { TagModule } from 'primeng/tag';
import { MenuItem } from 'primeng/api';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { ErrorStateComponent } from '../../../../shared/components/error-state/error-state.component';
import { CrawlerService } from '../services/crawler.service';
import { CertificadoStatus } from '../models/crawler.models';

@Component({
  selector: 'app-crawler-certificado',
  standalone: true,
  imports: [
    FormsModule, DatePipe, ButtonModule, InputTextModule, FileUploadModule, TagModule,
    PageHeaderComponent, LoadingSpinnerComponent, ErrorStateComponent,
  ],
  templateUrl: './crawler-certificado.component.html',
  styleUrl: './crawler-certificado.component.scss',
})
export class CrawlerCertificadoComponent implements OnInit {
  private readonly _crawlerService = inject(CrawlerService);

  readonly migalhas: MenuItem[] = [
    { label: 'Administração', routerLink: '/admin' },
    { label: 'Crawler', routerLink: '/admin/crawler' },
    { label: 'Certificado' },
  ];

  readonly statusCertificado = signal<CertificadoStatus | null>(null);
  readonly carregando = signal(true);
  readonly erro = signal('');

  readonly arquivoSelecionado = signal<File | null>(null);
  readonly senhaCertificado = signal('');
  readonly enviando = signal(false);
  readonly mensagemUpload = signal('');
  readonly erroUpload = signal('');

  readonly removendo = signal(false);

  readonly certificadoProximoDoVencimento = computed(() => {
    const status = this.statusCertificado();
    if (!status?.validoAte) return false;
    const validoAte = new Date(status.validoAte);
    const hoje = new Date();
    const diasRestantes = Math.ceil((validoAte.getTime() - hoje.getTime()) / (1000 * 60 * 60 * 24));
    return diasRestantes <= 30 && diasRestantes > 0;
  });

  readonly certificadoVencido = computed(() => {
    const status = this.statusCertificado();
    if (!status?.validoAte) return false;
    const validoAte = new Date(status.validoAte);
    return validoAte.getTime() < new Date().getTime();
  });

  readonly diasParaVencimento = computed(() => {
    const status = this.statusCertificado();
    if (!status?.validoAte) return null;
    const validoAte = new Date(status.validoAte);
    return Math.ceil((validoAte.getTime() - new Date().getTime()) / (1000 * 60 * 60 * 24));
  });

  ngOnInit(): void {
    this._carregarStatus();
  }

  tentarNovamente(): void {
    this._carregarStatus();
  }

  onArquivoSelecionado(evento: { files: File[] }): void {
    if (evento.files?.length > 0) {
      this.arquivoSelecionado.set(evento.files[0]);
    }
  }

  enviarCertificado(): void {
    const arquivo = this.arquivoSelecionado();
    const senha = this.senhaCertificado();

    if (!arquivo || !senha) return;

    this.enviando.set(true);
    this.mensagemUpload.set('');
    this.erroUpload.set('');

    this._crawlerService.uploadCertificado(arquivo, senha).subscribe({
      next: (resp) => {
        this.mensagemUpload.set(resp.mensagem);
        this.enviando.set(false);
        this.arquivoSelecionado.set(null);
        this.senhaCertificado.set('');
        this._carregarStatus();
      },
      error: (err) => {
        const msg = err.error?.erro ?? 'Erro ao enviar certificado.';
        this.erroUpload.set(msg);
        this.enviando.set(false);
      },
    });
  }

  removerCertificado(): void {
    this.removendo.set(true);
    this._crawlerService.removerCertificado().subscribe({
      next: () => {
        this.removendo.set(false);
        this._carregarStatus();
      },
      error: () => {
        this.removendo.set(false);
      },
    });
  }

  private _carregarStatus(): void {
    this.carregando.set(true);
    this.erro.set('');
    this._crawlerService.obterStatusCertificado().subscribe({
      next: (status) => {
        this.statusCertificado.set(status);
        this.carregando.set(false);
      },
      error: () => {
        this.erro.set('Erro ao carregar status do certificado.');
        this.carregando.set(false);
      },
    });
  }
}
