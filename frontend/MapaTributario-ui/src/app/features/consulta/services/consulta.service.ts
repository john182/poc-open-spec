import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  Estado,
  MunicipiosUfResponse,
  Aliquota,
  AliquotaDetalhe,
  RespostaPaginada,
  FiltroAliquota,
} from '../models/consulta.models';

@Injectable({ providedIn: 'root' })
export class ConsultaService {
  private readonly _http = inject(HttpClient);
  private readonly _baseUrl = '/api/v1';

  listarEstados(): Observable<Estado[]> {
    return this._http.get<Estado[]>(`${this._baseUrl}/estados`);
  }

  listarMunicipios(uf: string): Observable<MunicipiosUfResponse> {
    return this._http.get<MunicipiosUfResponse>(`${this._baseUrl}/estados/${uf}/municipios`);
  }

  listarAliquotas(codigoIbge: string, filtro?: FiltroAliquota): Observable<RespostaPaginada<Aliquota>> {
    let params = new HttpParams();

    if (filtro) {
      if (filtro.pagina) params = params.set('pagina', filtro.pagina);
      if (filtro.tamanhoPagina) params = params.set('tamanhoPagina', filtro.tamanhoPagina);
      if (filtro.codigoServico) params = params.set('codigoServico', filtro.codigoServico);
      if (filtro.descricao) params = params.set('descricao', filtro.descricao);
      if (filtro.aliquotaMin != null) params = params.set('aliquotaMin', filtro.aliquotaMin);
      if (filtro.aliquotaMax != null) params = params.set('aliquotaMax', filtro.aliquotaMax);
      if (filtro.competencia) params = params.set('competencia', filtro.competencia);
    }

    return this._http.get<RespostaPaginada<Aliquota>>(
      `${this._baseUrl}/municipios/${codigoIbge}/aliquotas`,
      { params },
    );
  }

  obterDetalheAliquota(codigoIbge: string, codigoServico: string): Observable<AliquotaDetalhe> {
    return this._http.get<AliquotaDetalhe>(
      `${this._baseUrl}/municipios/${codigoIbge}/aliquotas/${codigoServico}`,
    );
  }
}
