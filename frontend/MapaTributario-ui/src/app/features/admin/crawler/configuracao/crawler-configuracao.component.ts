import { Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { DatePipe } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { CheckboxModule } from 'primeng/checkbox';
import { TooltipModule } from 'primeng/tooltip';
import { DividerModule } from 'primeng/divider';
import { TextareaModule } from 'primeng/textarea';
import { MenuItem } from 'primeng/api';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { ErrorStateComponent } from '../../../../shared/components/error-state/error-state.component';
import { CrawlerService } from '../services/crawler.service';
import { ConfiguracaoCrawler, AtualizarConfiguracaoCrawlerRequest } from '../models/crawler.models';

@Component({
  selector: 'app-crawler-configuracao',
  standalone: true,
  imports: [
    FormsModule, RouterModule, DatePipe, ButtonModule, InputTextModule, InputNumberModule,
    CheckboxModule, TooltipModule, DividerModule, TextareaModule,
    PageHeaderComponent, LoadingSpinnerComponent, ErrorStateComponent,
  ],
  templateUrl: './crawler-configuracao.component.html',
  styleUrl: './crawler-configuracao.component.scss',
})
export class CrawlerConfiguracaoComponent implements OnInit {
  private readonly _crawlerService = inject(CrawlerService);

  readonly migalhas: MenuItem[] = [
    { label: 'Administração', routerLink: '/admin' },
    { label: 'Crawler', routerLink: '/admin/crawler' },
    { label: 'Configuração' },
  ];

  readonly configuracao = signal<ConfiguracaoCrawler | null>(null);
  readonly carregando = signal(true);
  readonly erro = signal('');
  readonly salvando = signal(false);
  readonly mensagemSucesso = signal('');
  readonly erroSalvar = signal('');

  // Campos do formulário (two-way binding via signals)
  readonly cronSchedule = signal('');
  readonly limiteRequisicoesPorSegundo = signal(15);
  readonly orcamentoDiario = signal(50000);
  readonly tamanhoLoteCertificado = signal(200);
  readonly pausaLoteSegundos = signal(5);
  readonly tamanhoLoteMongo = signal(50);
  readonly maxTentativas = signal(3);
  readonly limiteParadaAntecipada = signal(9);
  readonly maxDesdobramento = signal(20);
  readonly maxDetalhamento = signal(99);
  readonly maxFalhasConsecutivasDetalhamento = signal(2);
  readonly maxFalhasConsecutivasDesdobramento = signal(2);
  readonly maxItensParalelos = signal(10);
  readonly codigosSondagem = signal<string[]>([]);
  readonly codigosSondagemTexto = signal('');
  readonly validadeDiasProcessamento = signal(7);
  readonly circuitBreakerLimiarErroPercent = signal(50);
  readonly circuitBreakerJanelaAvaliacaoSegundos = signal(60);
  readonly circuitBreakerPausaSegundos = signal(300);
  readonly circuitBreakerAmostraMinima = signal(10);
  readonly ativo = signal(true);

  ngOnInit(): void {
    this._carregarConfiguracao();
  }

  tentarNovamente(): void {
    this._carregarConfiguracao();
  }

  salvar(): void {
    this.salvando.set(true);
    this.mensagemSucesso.set('');
    this.erroSalvar.set('');

    const codigosParseados = this.codigosSondagemTexto()
      .split(',')
      .map(c => c.trim())
      .filter(c => c.length > 0);

    const request: AtualizarConfiguracaoCrawlerRequest = {
      cronSchedule: this.cronSchedule(),
      limiteRequisicoesPorSegundo: this.limiteRequisicoesPorSegundo(),
      orcamentoDiario: this.orcamentoDiario(),
      tamanhoLoteCertificado: this.tamanhoLoteCertificado(),
      pausaLoteSegundos: this.pausaLoteSegundos(),
      tamanhoLoteMongo: this.tamanhoLoteMongo(),
      maxTentativas: this.maxTentativas(),
      limiteParadaAntecipada: this.limiteParadaAntecipada(),
      maxDesdobramento: this.maxDesdobramento(),
      maxDetalhamento: this.maxDetalhamento(),
      maxFalhasConsecutivasDetalhamento: this.maxFalhasConsecutivasDetalhamento(),
      maxFalhasConsecutivasDesdobramento: this.maxFalhasConsecutivasDesdobramento(),
      maxItensParalelos: this.maxItensParalelos(),
      codigosSondagem: codigosParseados,
      validadeDiasProcessamento: this.validadeDiasProcessamento(),
      circuitBreakerLimiarErroPercent: this.circuitBreakerLimiarErroPercent(),
      circuitBreakerJanelaAvaliacaoSegundos: this.circuitBreakerJanelaAvaliacaoSegundos(),
      circuitBreakerPausaSegundos: this.circuitBreakerPausaSegundos(),
      circuitBreakerAmostraMinima: this.circuitBreakerAmostraMinima(),
      ativo: this.ativo(),
    };

    this._crawlerService.atualizarConfiguracao(request).subscribe({
      next: (configuracaoAtualizada) => {
        this.configuracao.set(configuracaoAtualizada);
        this._preencherFormulario(configuracaoAtualizada);
        this.mensagemSucesso.set('Configuração salva com sucesso.');
        this.salvando.set(false);
      },
      error: (erro) => {
        const mensagem = erro.error?.erro ?? 'Erro ao salvar configuração.';
        const detalhes = erro.error?.detalhes;
        if (detalhes?.length) {
          this.erroSalvar.set(`${mensagem} ${detalhes.join('; ')}`);
        } else {
          this.erroSalvar.set(mensagem);
        }
        this.salvando.set(false);
      },
    });
  }

  restaurarPadrao(): void {
    this.mensagemSucesso.set('');
    this.erroSalvar.set('');
    this._carregarConfiguracao();
  }

  private _carregarConfiguracao(): void {
    this.carregando.set(true);
    this.erro.set('');
    this._crawlerService.obterConfiguracao().subscribe({
      next: (configuracao) => {
        this.configuracao.set(configuracao);
        this._preencherFormulario(configuracao);
        this.carregando.set(false);
      },
      error: () => {
        this.erro.set('Erro ao carregar configuração do crawler.');
        this.carregando.set(false);
      },
    });
  }

  private _preencherFormulario(configuracao: ConfiguracaoCrawler): void {
    this.cronSchedule.set(configuracao.cronSchedule);
    this.limiteRequisicoesPorSegundo.set(configuracao.limiteRequisicoesPorSegundo);
    this.orcamentoDiario.set(configuracao.orcamentoDiario);
    this.tamanhoLoteCertificado.set(configuracao.tamanhoLoteCertificado);
    this.pausaLoteSegundos.set(configuracao.pausaLoteSegundos);
    this.tamanhoLoteMongo.set(configuracao.tamanhoLoteMongo);
    this.maxTentativas.set(configuracao.maxTentativas);
    this.limiteParadaAntecipada.set(configuracao.limiteParadaAntecipada);
    this.maxDesdobramento.set(configuracao.maxDesdobramento);
    this.maxDetalhamento.set(configuracao.maxDetalhamento);
    this.maxFalhasConsecutivasDetalhamento.set(configuracao.maxFalhasConsecutivasDetalhamento);
    this.maxFalhasConsecutivasDesdobramento.set(configuracao.maxFalhasConsecutivasDesdobramento);
    this.maxItensParalelos.set(configuracao.maxItensParalelos);
    this.codigosSondagem.set([...configuracao.codigosSondagem]);
    this.codigosSondagemTexto.set(configuracao.codigosSondagem.join(', '));
    this.validadeDiasProcessamento.set(configuracao.validadeDiasProcessamento);
    this.circuitBreakerLimiarErroPercent.set(configuracao.circuitBreakerLimiarErroPercent);
    this.circuitBreakerJanelaAvaliacaoSegundos.set(configuracao.circuitBreakerJanelaAvaliacaoSegundos);
    this.circuitBreakerPausaSegundos.set(configuracao.circuitBreakerPausaSegundos);
    this.circuitBreakerAmostraMinima.set(configuracao.circuitBreakerAmostraMinima);
    this.ativo.set(configuracao.ativo);
  }
}
