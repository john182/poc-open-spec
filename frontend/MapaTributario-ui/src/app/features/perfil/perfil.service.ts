import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AtualizarPerfilRequest, AtualizarPerfilResponse, PerfilResponse } from './perfil.models';

@Injectable({ providedIn: 'root' })
export class PerfilService {
  private readonly _http = inject(HttpClient);
  private readonly _baseUrl = '/api/v1/perfil';

  obterPerfil(): Observable<PerfilResponse> {
    return this._http.get<PerfilResponse>(this._baseUrl);
  }

  atualizarPerfil(dados: AtualizarPerfilRequest): Observable<AtualizarPerfilResponse> {
    return this._http.put<AtualizarPerfilResponse>(this._baseUrl, dados);
  }
}
