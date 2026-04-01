import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { TableModule } from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { MenuItem } from 'primeng/api';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../../shared/components/error-state/error-state.component';
import { ConsultaService } from '../services/consulta.service';
import { Aliquota, AliquotaDetalhe, FiltroAliquota } from '../models/consulta.models';

@Component({
  selector: 'app-municipio-aliquotas',
  standalone: true,
  imports: [
    FormsModule, DecimalPipe, TableModule, InputTextModule, InputNumberModule,
    ButtonModule, DialogModule, PageHeaderComponent,
    LoadingSpinnerComponent, EmptyStateComponent, ErrorStateComponent,
  ],
  templateUrl: './municipio-aliquotas.component.html',
  styleUrl: './municipio-aliquotas.component.scss',
})
export class MunicipioAliquotasComponent implements OnInit {
  private readonly _route = inject(ActivatedRoute);
  private readonly _consultaService = inject(ConsultaService);

  readonly codigoIbge = signal('');
  readonly nomeMunicipio = signal('');
  readonly uf = signal('');
  readonly aliquotas = signal<Aliquota[]>([]);
  readonly carregando = signal(true);
  readonly erro = signal('');
  readonly totalItens = signal(0);
  readonly totalPaginas = signal(0);

  readonly filtro = signal<FiltroAliquota>({ pagina: 1, tamanhoPagina: 20 });
  readonly filtroCodigoServico = signal('');
  readonly filtroDescricao = signal('');
  readonly filtroAliquotaMin = signal<number | null>(null);
  readonly filtroAliquotaMax = signal<number | null>(null);
  readonly filtroCompetencia = signal('');

  readonly detalheVisivel = signal(false);
  readonly detalhe = signal<AliquotaDetalhe | null>(null);
  readonly detalheCarregando = signal(false);

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
    this._carregarAliquotas();
  }

  aplicarFiltros(): void {
    this.filtro.set({
      pagina: 1,
      tamanhoPagina: 20,
      codigoServico: this.filtroCodigoServico() || undefined,
      descricao: this.filtroDescricao() || undefined,
      aliquotaMin: this.filtroAliquotaMin() ?? undefined,
      aliquotaMax: this.filtroAliquotaMax() ?? undefined,
      competencia: this.filtroCompetencia() || undefined,
    });
    this._carregarAliquotas();
  }

  limparFiltros(): void {
    this.filtroCodigoServico.set('');
    this.filtroDescricao.set('');
    this.filtroAliquotaMin.set(null);
    this.filtroAliquotaMax.set(null);
    this.filtroCompetencia.set('');
    this.aplicarFiltros();
  }

  onPaginaChange(evento: { first: number; rows: number }): void {
    const pagina = Math.floor(evento.first / evento.rows) + 1;
    this.filtro.update(f => ({ ...f, pagina, tamanhoPagina: evento.rows }));
    this._carregarAliquotas();
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
    this._carregarAliquotas();
  }

  private _carregarAliquotas(): void {
    this.carregando.set(true);
    this.erro.set('');
    this._consultaService.listarAliquotas(this.codigoIbge(), this.filtro()).subscribe({
      next: (resposta) => {
        this.aliquotas.set(resposta.items);
        this.totalItens.set(resposta.totalItens);
        this.totalPaginas.set(resposta.totalPaginas);
        this.carregando.set(false);
      },
      error: () => {
        this.erro.set('Erro ao carregar alíquotas. Tente novamente.');
        this.carregando.set(false);
      },
    });
  }
}
