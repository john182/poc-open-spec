import { render, screen, waitFor } from '@testing-library/angular';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { CrawlerExecucoesComponent } from './crawler-execucoes.component';

describe('CrawlerExecucoesComponent', () => {
  async function setup() {
    const result = await render(CrawlerExecucoesComponent, {
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
    const httpTesting = TestBed.inject(HttpTestingController);
    return { ...result, httpTesting };
  }

  const execucaoConcluidaMock = {
    id: '1', inicio: '2026-03-01T10:00:00Z', fim: '2026-03-01T12:00:00Z',
    status: 'Concluido', tipo: 'Agendado', totalMunicipios: 100,
    totalServicos: 500, processados: 490, erros: 10, detalhesErro: [],
  };

  const execucaoErroMock = {
    id: '2', inicio: '2026-03-02T08:00:00Z', fim: '2026-03-02T08:30:00Z',
    status: 'Falha', tipo: 'Manual', totalMunicipios: 50,
    totalServicos: 200, processados: 100, erros: 100, detalhesErro: ['Erro de rede'],
  };

  const execucaoSemFimMock = {
    id: '3', inicio: '2026-03-03T10:00:00Z', fim: null,
    status: 'Executando', tipo: 'Agendado', totalMunicipios: 80,
    totalServicos: 300, processados: 50, erros: 0, detalhesErro: [],
  };

  it('deve exibir loading inicialmente', async () => {
    const { container } = await setup();
    expect(container.querySelector('app-loading-spinner')).toBeTruthy();
  });

  it('deve exibir empty state quando nao ha execucoes', async () => {
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/execucoes').flush([]);
    fixture.detectChanges();
    await waitFor(() => {
      expect(screen.getByText('Nenhuma execução encontrada')).toBeTruthy();
    });
  });

  it('deve exibir erro quando API falha', async () => {
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/execucoes').error(new ProgressEvent('error'));
    fixture.detectChanges();
    await waitFor(() => {
      expect(screen.getByText(/Erro ao carregar histórico de execuções/)).toBeTruthy();
    });
  });

  it('deve renderizar tabela quando ha execucoes', async () => {
    const { container, httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/execucoes').flush([execucaoConcluidaMock]);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    // Após carregar: sem empty state e sem erro
    expect(container.querySelector('app-empty-state')).toBeNull();
    expect(container.querySelector('app-error-state')).toBeNull();
  });

  it('Given_ExecucoesCarregadas_Should_ExibirTabelaComDados', async () => {
    // Arrange
    const { container, httpTesting, fixture } = await setup();

    // Act
    httpTesting.expectOne('/api/v1/crawler/execucoes').flush([execucaoConcluidaMock]);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    // Assert
    await waitFor(() => {
      expect(container.querySelector('[data-cy="tabela-execucoes"]')).toBeTruthy();
      expect(container.querySelector('[data-cy="execucao-1"]')).toBeTruthy();
    });
  });

  it('Given_MultiplasExecucoes_Should_ExibirTodasNaTabela', async () => {
    // Arrange
    const { container, httpTesting, fixture } = await setup();

    // Act
    httpTesting.expectOne('/api/v1/crawler/execucoes').flush([
      execucaoConcluidaMock,
      execucaoErroMock,
    ]);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    // Assert
    await waitFor(() => {
      expect(container.querySelector('[data-cy="execucao-1"]')).toBeTruthy();
      expect(container.querySelector('[data-cy="execucao-2"]')).toBeTruthy();
    });
  });

  it('Given_ObterSeveridadeStatus_Should_RetornarSeveridadeCorreta', async () => {
    // Arrange
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/execucoes').flush([]);
    fixture.detectChanges();

    const componente = fixture.componentInstance;

    // Act & Assert
    expect(componente.obterSeveridadeStatus('Concluido')).toBe('success');
    expect(componente.obterSeveridadeStatus('Executando')).toBe('info');
    expect(componente.obterSeveridadeStatus('Erro')).toBe('danger');
    expect(componente.obterSeveridadeStatus('Falha')).toBe('danger');
    expect(componente.obterSeveridadeStatus('Cancelado')).toBe('warn');
    expect(componente.obterSeveridadeStatus('OutroStatus')).toBe('secondary');
  });

  it('Given_TentarNovamente_Should_RecarregarExecucoes', async () => {
    // Arrange
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/execucoes').error(new ProgressEvent('error'));
    fixture.detectChanges();

    await waitFor(() => {
      expect(screen.getByText(/Erro ao carregar histórico de execuções/)).toBeTruthy();
    });

    // Act
    fixture.componentInstance.tentarNovamente();

    // Assert
    const reqRetry = httpTesting.expectOne('/api/v1/crawler/execucoes');
    reqRetry.flush([execucaoConcluidaMock]);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.execucoes().length).toBe(1);
    expect(fixture.componentInstance.erro()).toBe('');
  });

  it('Given_ExecucaoSemFim_Should_ExibirTracinho', async () => {
    // Arrange
    const { container, httpTesting, fixture } = await setup();

    // Act
    httpTesting.expectOne('/api/v1/crawler/execucoes').flush([execucaoSemFimMock]);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    // Assert
    await waitFor(() => {
      expect(container.querySelector('[data-cy="execucao-3"]')).toBeTruthy();
      expect(screen.getByText('—')).toBeTruthy();
    });
  });
});
