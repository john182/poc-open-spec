import { render, screen, waitFor } from '@testing-library/angular';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { ConsultaMapaComponent } from './consulta-mapa.component';

describe('ConsultaMapaComponent', () => {
  async function setup() {
    const result = await render(ConsultaMapaComponent, {
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

  it('deve exibir mapa apos carregar estados', async () => {
    const { container, httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/estados').flush([
      { codigo: 31, nome: 'Minas Gerais', sigla: 'MG', regiao: 'Sudeste' },
    ]);
    fixture.detectChanges();
    await waitFor(() => {
      expect(container.querySelector('[data-cy="brazil-map"]')).toBeTruthy();
      expect(screen.getByText('MG')).toBeTruthy();
      expect(screen.getByText('Minas Gerais')).toBeTruthy();
    });
  });

  it('deve exibir erro quando API falha', async () => {
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/estados').error(new ProgressEvent('error'));
    fixture.detectChanges();
    await waitFor(() => {
      expect(screen.getByText(/Erro ao carregar estados/)).toBeTruthy();
    });
  });
});
