import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  StatusCrawler,
  ExecutarCrawlerRequest,
  ExecutarCrawlerResponse,
  CertificadoStatus,
} from '../models/crawler.models';

@Injectable({ providedIn: 'root' })
export class CrawlerService {
  private readonly _http = inject(HttpClient);
  private readonly _baseUrl = '/api/v1/crawler';

  obterStatus(): Observable<StatusCrawler> {
    return this._http.get<StatusCrawler>(`${this._baseUrl}/status`);
  }

  executar(request: ExecutarCrawlerRequest): Observable<ExecutarCrawlerResponse> {
    return this._http.post<ExecutarCrawlerResponse>(`${this._baseUrl}/executar`, request);
  }

  listarExecucoes(): Observable<StatusCrawler[]> {
    return this._http.get<StatusCrawler[]>(`${this._baseUrl}/execucoes`);
  }

  obterStatusCertificado(): Observable<CertificadoStatus> {
    return this._http.get<CertificadoStatus>(`${this._baseUrl}/certificado`);
  }

  uploadCertificado(arquivo: File, senha: string): Observable<{ mensagem: string }> {
    const formData = new FormData();
    formData.append('arquivo', arquivo);
    formData.append('senha', senha);
    return this._http.post<{ mensagem: string }>(`${this._baseUrl}/certificado`, formData);
  }

  removerCertificado(): Observable<{ mensagem: string }> {
    return this._http.delete<{ mensagem: string }>(`${this._baseUrl}/certificado`);
  }
}
