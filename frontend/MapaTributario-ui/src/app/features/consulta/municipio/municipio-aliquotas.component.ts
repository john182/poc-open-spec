import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DecimalPipe, DatePipe } from '@angular/common';
import { TableLazyLoadEvent, TableModule } from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputMaskModule } from 'primeng/inputmask';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { DatePickerModule } from 'primeng/datepicker';
import { TooltipModule } from 'primeng/tooltip';
import { MenuItem } from 'primeng/api';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { ErrorStateComponent } from '../../../shared/components/error-state/error-state.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { ConsultaService } from '../services/consulta.service';
import { Aliquota, AliquotaDetalhe, FiltroAliquota, RespostaPaginada } from '../models/consulta.models';

export class Pagina<T> {
  totalElementos: number;
  totalPaginas: number;
  conteudo: Partial<T>[];

  constructor() {
    this.conteudo = [];
    this.totalElementos = 0;
    this.totalPaginas = 0;
  }
}

@Component({
  selector: 'app-municipio-aliquotas',
  standalone: true,
  imports: [
    FormsModule, DecimalPipe, DatePipe, TableModule, InputTextModule, InputNumberModule, InputMaskModule,
    ButtonModule, DialogModule, DatePickerModule, TooltipModule, PageHeaderComponent,
    ErrorStateComponent, LoadingSpinnerComponent,
  ],
  templateUrl: './municipio-aliquotas.component.html',
  styleUrl: './municipio-aliquotas.component.scss',
})
export class MunicipioAliquotasComponent implements OnInit {
  private readonly _route = inject(ActivatedRoute);
  private readonly _consultaService = inject(ConsultaService);

  // Dados do município (rota)
  readonly codigoIbge = signal('');
  readonly nomeMunicipio = signal('');
  readonly uf = signal('');

  // Estado da tabela (p-table lazy)
  readonly pagina = signal<Pagina<Aliquota>>(new Pagina<Aliquota>());
  readonly carregando = signal(false);
  readonly erro = signal('');

  // Filtros
  readonly filtroCodigoServico = signal('');
  readonly filtroDescricao = signal('');
  readonly filtroAliquotaMin = signal<number | null>(null);
  readonly filtroAliquotaMax = signal<number | null>(null);
  readonly filtroCompetencia = signal<Date | null>(null);

  // Detalhe
  readonly detalheVisivel = signal(false);
  readonly detalhe = signal<AliquotaDetalhe | null>(null);
  readonly detalheCarregando = signal(false);

  // Computeds
  readonly tituloMunicipio = computed(() =>
    this.nomeMunicipio() || `Município ${this.codigoIbge()}`
  );

  readonly migalhas = computed<MenuItem[]>(() => {
    const itens: MenuItem[] = [
      { label: 'Consulta', routerLink: '/consulta' },
    ];
    const uf = this.uf();
    if (uf) {
      itens.push({ label: uf, routerLink: `/consulta/estado/${uf}` });
    }
    itens.push({ label: this.tituloMunicipio() });
    return itens;
  });

  ngOnInit(): void {
    const codigo = this._route.snapshot.paramMap.get('codigoIbge') ?? '';
    const uf = this._route.snapshot.queryParamMap.get('uf') ?? '';
    const nome = this._route.snapshot.queryParamMap.get('nome') ?? '';
    this.codigoIbge.set(codigo);
    this.uf.set(uf);
    this.nomeMunicipio.set(nome);
  }

  /** Chamado pelo p-table (onLazyLoad) no init e a cada mudança de página/tamanho */
  pesquisarPaginada(evento?: TableLazyLoadEvent): void {
    const paginaIndex = (evento?.first ?? 0) / (evento?.rows ?? 20);
    const itensPorPagina = evento?.rows ?? 20;
    this._carregarDados(paginaIndex, itensPorPagina);
  }

  aplicarFiltros(): void {
    this._carregarDados(0, 20);
  }

  limparFiltros(): void {
    this.filtroCodigoServico.set('');
    this.filtroDescricao.set('');
    this.filtroAliquotaMin.set(null);
    this.filtroAliquotaMax.set(null);
    this.filtroCompetencia.set(null);
    this.aplicarFiltros();
  }

  verDetalhe(aliquota: Aliquota): void {
    this.detalheCarregando.set(true);
    this.detalheVisivel.set(true);
    this._consultaService.obterDetalheAliquota(this.codigoIbge(), aliquota.codigoServico).subscribe({
      next: (detalhe) => {
        this.detalhe.set(detalhe);
        this.detalheCarregando.set(false);
      },
      error: () => {
        this.detalheCarregando.set(false);
        this.detalheVisivel.set(false);
      },
    });
  }

  tentarNovamente(): void {
    this._carregarDados(0, 20);
  }

  // --- Privados ---

  private _carregarDados(paginaIndex: number, itensPorPagina: number): void {
    this.carregando.set(true);
    this.erro.set('');

    const filtro = this._montarFiltro(paginaIndex, itensPorPagina);

    this._consultaService.listarAliquotas(this.codigoIbge(), filtro)
      .subscribe({
        next: (resposta) => {
          this.pagina.set(this._mapearResposta(resposta));
        },
        error: () => {
          this.erro.set('Erro ao carregar alíquotas. Tente novamente.');
        },
      })
      .add(() => {
        this.carregando.set(false);
      });
  }

  private _montarFiltro(paginaIndex: number, itensPorPagina: number): FiltroAliquota {
    const codigoServicoRaw = this.filtroCodigoServico()?.replace(/_/g, '').replace(/\.+$/, '') || '';

    let competenciaStr: string | undefined;
    const competenciaDate = this.filtroCompetencia();
    if (competenciaDate) {
      const ano = competenciaDate.getFullYear();
      const mes = String(competenciaDate.getMonth() + 1).padStart(2, '0');
      competenciaStr = `${ano}-${mes}`;
    }

    return {
      pagina: paginaIndex,
      tamanhoPagina: itensPorPagina,
      codigoServico: codigoServicoRaw || undefined,
      descricao: this.filtroDescricao() || undefined,
      aliquotaMin: this.filtroAliquotaMin() ?? undefined,
      aliquotaMax: this.filtroAliquotaMax() ?? undefined,
      competencia: competenciaStr,
    };
  }

  private _mapearResposta<T>(resposta: RespostaPaginada<T>): Pagina<T> {
    const pag = new Pagina<T>();
    pag.conteudo = resposta.items;
    pag.totalElementos = resposta.totalItens;
    pag.totalPaginas = resposta.totalPaginas;
    return pag;
  }
}
