import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { CrawlerService } from './crawler.service';

describe('CrawlerService', () => {
  let service: CrawlerService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(CrawlerService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('deve obter status do crawler', () => {
    const statusMock = {
      id: '1', inicio: '2026-03-01T10:00:00Z', fim: null, status: 'Executando',
      tipo: 'Manual', totalMunicipios: 10, totalServicos: 50,
      processados: 30, erros: 2, detalhesErro: [],
    };
    service.obterStatus().subscribe(status => {
      expect(status).toEqual(statusMock);
    });
    const req = httpTesting.expectOne('/api/v1/crawler/status');
    expect(req.request.method).toBe('GET');
    req.flush(statusMock);
  });

  it('deve executar crawler', () => {
    const respostaMock = { mensagem: 'Execucao iniciada com sucesso' };
    service.executar({ forcarReprocessamento: true, ufs: ['MG'] }).subscribe(resp => {
      expect(resp).toEqual(respostaMock);
    });
    const req = httpTesting.expectOne('/api/v1/crawler/executar');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ forcarReprocessamento: true, ufs: ['MG'] });
    req.flush(respostaMock);
  });

  it('deve listar execucoes', () => {
    const execucoesMock = [
      {
        id: '1', inicio: '2026-03-01T10:00:00Z', fim: '2026-03-01T12:00:00Z',
        status: 'Concluido', tipo: 'Agendado', totalMunicipios: 100,
        totalServicos: 500, processados: 490, erros: 10, detalhesErro: [],
      },
    ];
    service.listarExecucoes().subscribe(execucoes => {
      expect(execucoes).toEqual(execucoesMock);
    });
    const req = httpTesting.expectOne('/api/v1/crawler/execucoes');
    expect(req.request.method).toBe('GET');
    req.flush(execucoesMock);
  });

  it('deve obter status do certificado', () => {
    const statusMock = { hasCertificate: true, uploadedAt: '2026-03-01T10:00:00Z' };
    service.obterStatusCertificado().subscribe(status => {
      expect(status).toEqual(statusMock);
    });
    const req = httpTesting.expectOne('/api/v1/crawler/certificado');
    expect(req.request.method).toBe('GET');
    req.flush(statusMock);
  });

  it('deve enviar certificado via FormData', () => {
    const arquivo = new File(['pfx-content'], 'cert.pfx', { type: 'application/x-pkcs12' });
    const respostaMock = { mensagem: 'Certificado armazenado com sucesso' };
    service.uploadCertificado(arquivo, 'senha123').subscribe(resp => {
      expect(resp).toEqual(respostaMock);
    });
    const req = httpTesting.expectOne('/api/v1/crawler/certificado');
    expect(req.request.method).toBe('POST');
    expect(req.request.body instanceof FormData).toBe(true);
    req.flush(respostaMock);
  });

  it('deve remover certificado', () => {
    const respostaMock = { mensagem: 'Certificado removido com sucesso' };
    service.removerCertificado().subscribe(resp => {
      expect(resp).toEqual(respostaMock);
    });
    const req = httpTesting.expectOne('/api/v1/crawler/certificado');
    expect(req.request.method).toBe('DELETE');
    req.flush(respostaMock);
  });
});
