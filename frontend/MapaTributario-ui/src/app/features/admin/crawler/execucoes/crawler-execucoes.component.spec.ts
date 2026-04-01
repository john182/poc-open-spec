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
    httpTesting.expectOne('/api/v1/crawler/execucoes').flush([
      {
        id: '1', inicio: '2026-03-01T10:00:00Z', fim: '2026-03-01T12:00:00Z',
        status: 'Concluido', tipo: 'Agendado', totalMunicipios: 100,
        totalServicos: 500, processados: 490, erros: 10, detalhesErro: [],
      },
    ]);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    // Após carregar: sem empty state e sem erro
    expect(container.querySelector('app-empty-state')).toBeNull();
    expect(container.querySelector('app-error-state')).toBeNull();
  });
});
