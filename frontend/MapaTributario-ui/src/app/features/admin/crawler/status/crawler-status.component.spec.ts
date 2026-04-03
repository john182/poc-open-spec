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
      expect(screen.getByText('Concluído')).toBeTruthy();
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

  it('Given_CliqueBotaoExecutar_Should_EnviarRequisicaoEExibirMensagem', async () => {
    // Arrange
    const { container, httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/status').flush(statusMock);
    fixture.detectChanges();

    await waitFor(() => {
      expect(container.querySelector('[data-cy="btn-executar"] button')).toBeTruthy();
    });

    const botaoExecutar = container.querySelector('[data-cy="btn-executar"] button') as HTMLButtonElement;

    // Act
    fireEvent.click(botaoExecutar);

    // Assert
    const reqExecutar = httpTesting.expectOne('/api/v1/crawler/executar');
    expect(reqExecutar.request.method).toBe('POST');
    expect(reqExecutar.request.body).toEqual({
      forcarReprocessamento: false,
      ufs: undefined,
    });
    reqExecutar.flush({ mensagem: 'Crawler iniciado com sucesso' });

    httpTesting.expectOne('/api/v1/crawler/status').flush(statusMock);
    fixture.detectChanges();

    await waitFor(() => {
      expect(screen.getByText('Crawler iniciado com sucesso')).toBeTruthy();
    });
  });

  it('Given_FalhaAoExecutarCrawler_Should_ExibirMensagemErro', async () => {
    // Arrange
    const { container, httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/status').flush(statusMock);
    fixture.detectChanges();

    await waitFor(() => {
      expect(container.querySelector('[data-cy="btn-executar"] button')).toBeTruthy();
    });

    const botaoExecutar = container.querySelector('[data-cy="btn-executar"] button') as HTMLButtonElement;

    // Act
    fireEvent.click(botaoExecutar);

    httpTesting.expectOne('/api/v1/crawler/executar').flush(
      { erro: 'Crawler já em execução' },
      { status: 409, statusText: 'Conflict' },
    );
    fixture.detectChanges();

    // Assert
    await waitFor(() => {
      expect(screen.getByText('Crawler já em execução')).toBeTruthy();
    });
    expect(fixture.componentInstance.executando()).toBe(false);
  });

  it('Given_FalhaAoExecutarSemDetalhe_Should_ExibirMensagemPadrao', async () => {
    // Arrange
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/status').flush(statusMock);
    fixture.detectChanges();

    const componente = fixture.componentInstance;

    // Act
    componente.executarCrawler();

    httpTesting.expectOne('/api/v1/crawler/executar').error(new ProgressEvent('error'));
    fixture.detectChanges();

    // Assert
    expect(componente.erroExecucao()).toBe('Erro ao iniciar execução do crawler.');
    expect(componente.executando()).toBe(false);
  });

  it('Given_SemCertificado_Should_ExibirAvisoDeCertificado', async () => {
    // Arrange
    const { container, httpTesting, fixture } = await setup();

    // Act
    httpTesting.expectOne('/api/v1/crawler/status').flush({
      ...statusMock,
      temCertificado: false,
    });
    fixture.detectChanges();

    // Assert
    await waitFor(() => {
      expect(container.querySelector('[data-cy="aviso-certificado"]')).toBeTruthy();
      expect(screen.getByText(/Nenhum certificado digital configurado/)).toBeTruthy();
    });
  });

  it('Given_ComCertificado_Should_NaoExibirAvisoDeCertificado', async () => {
    // Arrange
    const { container, httpTesting, fixture } = await setup();

    // Act
    httpTesting.expectOne('/api/v1/crawler/status').flush(statusMock);
    fixture.detectChanges();

    // Assert
    await waitFor(() => {
      expect(container.querySelector('[data-cy="aviso-certificado"]')).toBeNull();
    });
  });

  it('Given_StatusComDetalhesErro_Should_ExibirListaDeErros', async () => {
    // Arrange
    const { container, httpTesting, fixture } = await setup();

    // Act
    httpTesting.expectOne('/api/v1/crawler/status').flush({
      ...statusMock,
      detalhesErro: ['Timeout no município X', 'Certificado expirado'],
    });
    fixture.detectChanges();

    // Assert
    await waitFor(() => {
      expect(container.querySelector('[data-cy="detalhes-erro"]')).toBeTruthy();
      expect(screen.getByText('Timeout no município X')).toBeTruthy();
      expect(screen.getByText('Certificado expirado')).toBeTruthy();
    });
  });

  it('Given_StatusSemDetalhesErro_Should_NaoExibirSecaoDeErros', async () => {
    // Arrange
    const { container, httpTesting, fixture } = await setup();

    // Act
    httpTesting.expectOne('/api/v1/crawler/status').flush(statusMock);
    fixture.detectChanges();

    // Assert
    await waitFor(() => {
      expect(container.querySelector('[data-cy="detalhes-erro"]')).toBeNull();
    });
  });

  it('Given_StatusEmAndamentoSemFim_Should_ExibirEmAndamento', async () => {
    // Arrange
    const { httpTesting, fixture } = await setup();

    // Act
    httpTesting.expectOne('/api/v1/crawler/status').flush({
      ...statusMock,
      status: 'EmAndamento',
      fim: null,
    });
    fixture.detectChanges();

    // Assert
    await waitFor(() => {
      expect(screen.getAllByText('Em andamento').length).toBeGreaterThanOrEqual(1);
    });

    // Limpar polling
    fixture.componentInstance.ngOnDestroy();
  });

  it('Given_ObterSeveridadeStatus_Should_RetornarSeveridadeCorreta', async () => {
    // Arrange
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/status').flush(statusMock);
    fixture.detectChanges();

    const componente = fixture.componentInstance;

    // Act & Assert
    expect(componente.obterSeveridadeStatus('Concluido')).toBe('success');
    expect(componente.obterSeveridadeStatus('EmAndamento')).toBe('info');
    expect(componente.obterSeveridadeStatus('FalhaParcial')).toBe('warn');
    expect(componente.obterSeveridadeStatus('Falha')).toBe('danger');
    expect(componente.obterSeveridadeStatus('NenhumaExecucao')).toBe('secondary');
    expect(componente.obterSeveridadeStatus('OutroValor')).toBe('secondary');
  });

  it('Given_ObterSeveridadeProgressoUf_Should_RetornarSeveridadeCorreta', async () => {
    // Arrange
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/status').flush(statusMock);
    fixture.detectChanges();

    const componente = fixture.componentInstance;

    // Act & Assert
    expect(componente.obterSeveridadeProgressoUf('Concluido')).toBe('success');
    expect(componente.obterSeveridadeProgressoUf('EmAndamento')).toBe('info');
    expect(componente.obterSeveridadeProgressoUf('Falha')).toBe('danger');
    expect(componente.obterSeveridadeProgressoUf('OutroValor')).toBe('secondary');
  });

  it('Given_TentarNovamente_Should_RecarregarStatus', async () => {
    // Arrange
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/status').error(new ProgressEvent('error'));
    fixture.detectChanges();

    await waitFor(() => {
      expect(screen.getByText(/Erro ao carregar status do crawler/)).toBeTruthy();
    });

    // Act
    fixture.componentInstance.tentarNovamente();

    // Assert
    const reqRetry = httpTesting.expectOne('/api/v1/crawler/status');
    reqRetry.flush(statusMock);
    fixture.detectChanges();

    await waitFor(() => {
      expect(screen.getByText('Concluído')).toBeTruthy();
    });
  });
});
