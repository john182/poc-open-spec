import { render, screen, waitFor } from '@testing-library/angular';
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

  it('deve exibir empty state quando nao ha execucoes (404)', async () => {
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/status').flush(
      { erro: 'Nenhuma execucao encontrada' },
      { status: 404, statusText: 'Not Found' },
    );
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
});
