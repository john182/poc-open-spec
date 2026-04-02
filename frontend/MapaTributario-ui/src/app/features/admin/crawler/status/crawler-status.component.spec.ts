import { render, screen, waitFor, fireEvent } from '@testing-library/angular';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { CrawlerStatusComponent } from './crawler-status.component';

describe('CrawlerStatusComponent', () => {
  async function setup() {
    const result = await render(CrawlerStatusComponent, {
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
    const httpTesting = TestBed.inject(HttpTestingController);
    return { ...result, httpTesting };
  }

  const statusMock = {
    id: '1',
    inicio: '2026-03-01T10:00:00Z',
    fim: '2026-03-01T12:00:00Z',
    status: 'Concluido',
    tipo: 'Manual',
    totalMunicipios: 10,
    totalServicos: 50,
    processados: 48,
    erros: 2,
    detalhesErro: [],
    temCertificado: true,
    ufAtual: null,
    ufsProcessadas: ['SP', 'RJ'],
    progressoUfs: {},
  };

  it('deve exibir loading inicialmente', async () => {
    const { container } = await setup();
    expect(container.querySelector('app-loading-spinner')).toBeTruthy();
  });

  it('deve exibir metricas apos carregar status', async () => {
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/status').flush(statusMock);
    fixture.detectChanges();
    await waitFor(() => {
      expect(screen.getByText('Concluido')).toBeTruthy();
      expect(screen.getByText('Manual')).toBeTruthy();
      expect(screen.getByText('48')).toBeTruthy();
    });
  });

  it('deve exibir empty state quando nenhuma execucao encontrada', async () => {
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/status').flush({
      id: '',
      inicio: '0001-01-01T00:00:00',
      fim: null,
      status: 'NenhumaExecucao',
      tipo: '',
      totalMunicipios: 0,
      totalServicos: 0,
      processados: 0,
      erros: 0,
      detalhesErro: [],
      temCertificado: false,
      ufAtual: null,
      ufsProcessadas: [],
      progressoUfs: {},
    });
    fixture.detectChanges();
    await waitFor(() => {
      expect(screen.getByText('Nenhuma execução encontrada')).toBeTruthy();
    });
  });

  it('deve exibir erro quando API falha', async () => {
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/status').error(new ProgressEvent('error'));
    fixture.detectChanges();
    await waitFor(() => {
      expect(screen.getByText(/Erro ao carregar status do crawler/)).toBeTruthy();
    });
  });

  it('deve exibir secao de execucao manual', async () => {
    const { container, httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/status').flush(statusMock);
    fixture.detectChanges();
    await waitFor(() => {
      expect(container.querySelector('[data-cy="executar-crawler"]')).toBeTruthy();
      expect(container.querySelector('[data-cy="btn-executar"]')).toBeTruthy();
    });
  });

  it('deve exibir progresso por UF quando disponivel', async () => {
    const { container, httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/status').flush({
      ...statusMock,
      status: 'EmAndamento',
      fim: null,
      ufAtual: 'MG',
      ufsProcessadas: ['SP', 'RJ', 'MG'],
      progressoUfs: {
        SP: { uf: 'SP', status: 'Concluido', municipiosEncontrados: 645, inicio: '2026-03-01T10:00:00Z', fim: '2026-03-01T10:30:00Z' },
        RJ: { uf: 'RJ', status: 'Concluido', municipiosEncontrados: 92, inicio: '2026-03-01T10:30:00Z', fim: '2026-03-01T10:45:00Z' },
        MG: { uf: 'MG', status: 'EmAndamento', municipiosEncontrados: 200, inicio: '2026-03-01T10:45:00Z', fim: null },
      },
    });
    fixture.detectChanges();
    await waitFor(() => {
      expect(container.querySelector('[data-cy="progresso-ufs"]')).toBeTruthy();
      expect(container.querySelector('[data-cy="progresso-uf-SP"]')).toBeTruthy();
      expect(container.querySelector('[data-cy="progresso-uf-RJ"]')).toBeTruthy();
      expect(container.querySelector('[data-cy="progresso-uf-MG"]')).toBeTruthy();
      // UFs concluídas (tags verdes) devem ser apenas SP e RJ (status Concluido no progressoUfs)
      expect(fixture.componentInstance.ufsConcluidasLista()).toEqual(['SP', 'RJ']);
    });
  });

  it('nao deve exibir tags verdes para UFs com status EmAndamento', async () => {
    const { container, httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/status').flush({
      ...statusMock,
      status: 'EmAndamento',
      fim: null,
      ufAtual: 'SP',
      ufsProcessadas: ['SP'],
      progressoUfs: {
        SP: { uf: 'SP', status: 'EmAndamento', municipiosEncontrados: 100, inicio: '2026-03-01T10:00:00Z', fim: null },
      },
    });
    fixture.detectChanges();
    await waitFor(() => {
      expect(container.querySelector('[data-cy="progresso-ufs"]')).toBeTruthy();
      // Nenhuma UF concluída — não deve exibir tags verdes
      expect(fixture.componentInstance.ufsConcluidasLista()).toEqual([]);
    });
  });

  it('deve iniciar polling quando status for EmAndamento', async () => {
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/status').flush({
      ...statusMock,
      status: 'EmAndamento',
      fim: null,
      ufAtual: 'SP',
      ufsProcessadas: [],
      progressoUfs: {},
    });
    fixture.detectChanges();

    expect(fixture.componentInstance['_intervalPolling']).toBeTruthy();

    fixture.componentInstance.ngOnDestroy();
    expect(fixture.componentInstance['_intervalPolling']).toBeNull();
  });

  it('nao deve iniciar polling quando status for Concluido', async () => {
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/status').flush(statusMock);
    fixture.detectChanges();

    expect(fixture.componentInstance['_intervalPolling']).toBeNull();
  });

  it('Given_StatusCarregado_Should_ExibirBotaoExecutarCapitaisPrimeiro', async () => {
    // Arrange
    const { container, httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/status').flush(statusMock);
    fixture.detectChanges();

    // Act — nenhuma ação necessária, apenas verificar presença

    // Assert
    await waitFor(() => {
      expect(container.querySelector('[data-cy="btn-executar-capitais"]')).toBeTruthy();
    });
  });

  it('Given_CliqueBotaoCapitais_Should_EnviarCapitaisPrimeiroTrueEExibirMensagemSucesso', async () => {
    // Arrange
    const { container, httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/status').flush(statusMock);
    fixture.detectChanges();

    await waitFor(() => {
      expect(container.querySelector('[data-cy="btn-executar-capitais"] button')).toBeTruthy();
    });

    const botaoCapitais = container.querySelector('[data-cy="btn-executar-capitais"] button') as HTMLButtonElement;

    // Act
    fireEvent.click(botaoCapitais);

    // Assert — verificar requisição POST com body correto
    const reqExecutar = httpTesting.expectOne('/api/v1/crawler/executar');
    expect(reqExecutar.request.method).toBe('POST');
    expect(reqExecutar.request.body).toEqual({
      forcarReprocessamento: false,
      capitaisPrimeiro: true,
    });
    reqExecutar.flush({ mensagem: 'Crawler iniciado' });

    // Após sucesso, o componente chama _carregarStatus() novamente
    httpTesting.expectOne('/api/v1/crawler/status').flush(statusMock);
    fixture.detectChanges();

    await waitFor(() => {
      expect(screen.getByText('Crawler iniciado')).toBeTruthy();
    });
  });

  it('Given_FalhaAoExecutarCapitais_Should_ExibirMensagemDeErro', async () => {
    // Arrange
    const { container, httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/status').flush(statusMock);
    fixture.detectChanges();

    await waitFor(() => {
      expect(container.querySelector('[data-cy="btn-executar-capitais"] button')).toBeTruthy();
    });

    const botaoCapitais = container.querySelector('[data-cy="btn-executar-capitais"] button') as HTMLButtonElement;

    // Act
    fireEvent.click(botaoCapitais);

    // Assert — simular erro na requisição POST
    const reqExecutar = httpTesting.expectOne('/api/v1/crawler/executar');
    reqExecutar.flush(
      { erro: 'Falha interna no servidor' },
      { status: 500, statusText: 'Internal Server Error' },
    );
    fixture.detectChanges();

    await waitFor(() => {
      expect(screen.getByText('Falha interna no servidor')).toBeTruthy();
    });
  });
});
