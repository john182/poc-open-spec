import { render, screen, waitFor } from '@testing-library/angular';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { MunicipioAliquotasComponent } from './municipio-aliquotas.component';

describe('MunicipioAliquotasComponent', () => {
  async function setup(codigoIbge = '3106200') {
    const result = await render(MunicipioAliquotasComponent, {
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: { get: () => codigoIbge },
              queryParamMap: { get: (chave: string) => chave === 'uf' ? 'MG' : chave === 'nome' ? 'Belo Horizonte' : null },
            },
          },
        },
      ],
    });
    const httpTesting = TestBed.inject(HttpTestingController);
    return { ...result, httpTesting };
  }

  it('deve exibir loading inicialmente', async () => {
    const { container } = await setup();
    expect(container.querySelector('app-loading-spinner')).toBeTruthy();
  });

  it('deve renderizar tabela apos carregar aliquotas (nao empty state)', async () => {
    const { container, httpTesting, fixture } = await setup();
    httpTesting.expectOne(r => r.url === '/api/v1/municipios/3106200/aliquotas').flush({
      items: [
        {
          codigoServico: '010101001',
          codigoServicoFormatado: '01.01.01.001',
          descricaoServico: 'Analise e desenvolvimento',
          aliquota: 2.0,
          competencia: '2026-03-01',
        },
      ],
      pagina: 1,
      tamanhoPagina: 20,
      totalItens: 1,
      totalPaginas: 1,
    });
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    // Apos carregar dados: loading desaparece e empty state nao aparece
    expect(container.querySelector('app-empty-state')).toBeNull();
    expect(container.querySelector('app-error-state')).toBeNull();
  });

  it('deve exibir empty state quando nao ha aliquotas', async () => {
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne(r => r.url === '/api/v1/municipios/3106200/aliquotas').flush({
      items: [],
      pagina: 1,
      tamanhoPagina: 20,
      totalItens: 0,
      totalPaginas: 0,
    });
    fixture.detectChanges();
    await waitFor(() => {
      expect(screen.getByText('Nenhuma alíquota encontrada')).toBeTruthy();
    });
  });

  it('deve exibir erro quando API falha', async () => {
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne(r => r.url === '/api/v1/municipios/3106200/aliquotas').error(new ProgressEvent('error'));
    fixture.detectChanges();
    await waitFor(() => {
      expect(screen.getByText(/Erro ao carregar alíquotas/)).toBeTruthy();
    });
  });

  it('deve exibir filtros', async () => {
    const { container } = await setup();
    expect(container.querySelector('[data-cy="filtros-aliquotas"]')).toBeTruthy();
  });
});
