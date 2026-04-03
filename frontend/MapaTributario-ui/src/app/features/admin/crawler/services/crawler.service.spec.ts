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

  it('deve obter configuracao do crawler', () => {
    const configuracaoMock = {
      id: '1',
      cronSchedule: '0 2 * * *',
      limiteRequisicoesPorSegundo: 15,
      limiteDiarioRequisicoes: 50000,
      tamanhoLoteCertificado: 200,
      pausaLoteSegundos: 5,
      tamanhoLoteMongo: 50,
      maxTentativas: 3,
      limiteParadaAntecipada: 9,
      maxDesdobramento: 20,
      maxDetalhamento: 99,
      maxFalhasConsecutivasDetalhamento: 2,
      maxFalhasConsecutivasDesdobramento: 2,
      maxItensParalelos: 10,
      codigosSondagem: ['01.01.01', '07.02.01'],
      validadeDiasProcessamento: 7,
      circuitBreakerLimiarErroPercent: 50,
      circuitBreakerJanelaAvaliacaoSegundos: 60,
      circuitBreakerPausaSegundos: 300,
      circuitBreakerAmostraMinima: 10,
      ativo: true,
      criadoEm: '2026-03-01T10:00:00Z',
      atualizadoEm: '2026-03-01T10:00:00Z',
    };
    service.obterConfiguracao().subscribe(configuracao => {
      expect(configuracao).toEqual(configuracaoMock);
    });
    const req = httpTesting.expectOne('/api/v1/crawler/configuracao');
    expect(req.request.method).toBe('GET');
    req.flush(configuracaoMock);
  });

  it('deve atualizar configuracao do crawler', () => {
    const requestMock = {
      cronSchedule: '0 3 * * *',
      limiteRequisicoesPorSegundo: 20,
      limiteDiarioRequisicoes: 60000,
      tamanhoLoteCertificado: 250,
      pausaLoteSegundos: 10,
      tamanhoLoteMongo: 100,
      maxTentativas: 5,
      limiteParadaAntecipada: 12,
      maxDesdobramento: 30,
      maxDetalhamento: 50,
      maxFalhasConsecutivasDetalhamento: 3,
      maxFalhasConsecutivasDesdobramento: 3,
      maxItensParalelos: 15,
      codigosSondagem: ['01.01.01'],
      validadeDiasProcessamento: 14,
      circuitBreakerLimiarErroPercent: 60,
      circuitBreakerJanelaAvaliacaoSegundos: 120,
      circuitBreakerPausaSegundos: 600,
      circuitBreakerAmostraMinima: 20,
      ativo: false,
    };
    const respostaMock = {
      ...requestMock,
      id: '1',
      criadoEm: '2026-03-01T10:00:00Z',
      atualizadoEm: '2026-03-02T10:00:00Z',
    };
    service.atualizarConfiguracao(requestMock).subscribe(configuracao => {
      expect(configuracao).toEqual(respostaMock);
    });
    const req = httpTesting.expectOne('/api/v1/crawler/configuracao');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(requestMock);
    req.flush(respostaMock);
  });
});
