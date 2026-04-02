import { render, screen, waitFor } from '@testing-library/angular';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { EstadoMunicipiosComponent } from './estado-municipios.component';
import { MunicipiosUfResponse } from '../models/consulta.models';

describe('EstadoMunicipiosComponent', () => {
  async function setup(uf = 'MG') {
    const result = await render(EstadoMunicipiosComponent, {
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => uf } } },
        },
      ],
    });
    const httpTesting = TestBed.inject(HttpTestingController);
    return { ...result, httpTesting };
  }

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('deve exibir loading inicialmente', async () => {
    const { container } = await setup();
    expect(container.querySelector('app-loading-spinner')).toBeTruthy();
  });

  it('deve exibir municipios quando status concluido', async () => {
    const { httpTesting, fixture } = await setup();
    const resposta: MunicipiosUfResponse = {
      statusProcessamento: 'concluido',
      ultimoProcessamento: '2026-03-28T10:00:00Z',
      municipios: [
        { codigoIbge: '3106200', nome: 'Belo Horizonte', siglaEstado: 'MG', possuiAliquotas: true },
        { codigoIbge: '3118601', nome: 'Contagem', siglaEstado: 'MG', possuiAliquotas: true },
      ],
      semCertificado: false,
    };
    httpTesting.expectOne('/api/v1/estados/MG/municipios').flush(resposta);
    fixture.detectChanges();
    await waitFor(() => {
      expect(screen.getByText('Belo Horizonte')).toBeTruthy();
      expect(screen.getByText('Contagem')).toBeTruthy();
      expect(screen.getByText('2 municípios')).toBeTruthy();
    });
  });

  it('deve exibir mensagem quando processamento iniciado', async () => {
    vi.useFakeTimers();
    const { httpTesting, fixture } = await setup();
    const resposta: MunicipiosUfResponse = {
      statusProcessamento: 'processamentoIniciado',
      ultimoProcessamento: null,
      municipios: [],
      semCertificado: false,
    };
    httpTesting.expectOne('/api/v1/estados/MG/municipios').flush(resposta);
    fixture.detectChanges();
    await waitFor(() => {
      expect(screen.getByText(/processamento dos dados deste estado foi iniciado/)).toBeTruthy();
      expect(screen.getByText(/atualizada automaticamente/)).toBeTruthy();
    });
    fixture.componentInstance.ngOnDestroy();
    vi.useRealTimers();
  });

  it('deve exibir mensagem quando aguardando processamento', async () => {
    vi.useFakeTimers();
    const { httpTesting, fixture } = await setup();
    const resposta: MunicipiosUfResponse = {
      statusProcessamento: 'aguardandoProcessamento',
      ultimoProcessamento: null,
      municipios: [],
      semCertificado: false,
    };
    httpTesting.expectOne('/api/v1/estados/MG/municipios').flush(resposta);
    fixture.detectChanges();
    await waitFor(() => {
      expect(screen.getByText(/fila de processamento/)).toBeTruthy();
    });
    fixture.componentInstance.ngOnDestroy();
    vi.useRealTimers();
  });

  it('deve exibir mensagem quando processando sem dados', async () => {
    vi.useFakeTimers();
    const { httpTesting, fixture } = await setup();
    const resposta: MunicipiosUfResponse = {
      statusProcessamento: 'processando',
      ultimoProcessamento: null,
      municipios: [],
      semCertificado: false,
    };
    httpTesting.expectOne('/api/v1/estados/MG/municipios').flush(resposta);
    fixture.detectChanges();
    await waitFor(() => {
      expect(screen.getByText(/estão sendo processados/)).toBeTruthy();
    });
    fixture.componentInstance.ngOnDestroy();
    vi.useRealTimers();
  });

  it('deve exibir dados parciais quando processando com municipios', async () => {
    vi.useFakeTimers();
    const { httpTesting, fixture } = await setup();
    const resposta: MunicipiosUfResponse = {
      statusProcessamento: 'processando',
      ultimoProcessamento: null,
      municipios: [
        { codigoIbge: '3106200', nome: 'Belo Horizonte', siglaEstado: 'MG', possuiAliquotas: true },
      ],
      semCertificado: false,
    };
    httpTesting.expectOne('/api/v1/estados/MG/municipios').flush(resposta);
    fixture.detectChanges();
    await waitFor(() => {
      expect(screen.getByText('Belo Horizonte')).toBeTruthy();
      expect(screen.getByText(/parciais/)).toBeTruthy();
    });
    fixture.componentInstance.ngOnDestroy();
    vi.useRealTimers();
  });

  it('deve exibir aviso quando dados vencidos', async () => {
    const { httpTesting, fixture } = await setup();
    const resposta: MunicipiosUfResponse = {
      statusProcessamento: 'vencido',
      ultimoProcessamento: '2026-03-20T10:00:00Z',
      municipios: [
        { codigoIbge: '3106200', nome: 'Belo Horizonte', siglaEstado: 'MG', possuiAliquotas: true },
      ],
      semCertificado: false,
    };
    httpTesting.expectOne('/api/v1/estados/MG/municipios').flush(resposta);
    fixture.detectChanges();
    await waitFor(() => {
      expect(screen.getByText(/podem estar desatualizados/)).toBeTruthy();
      expect(screen.getByText('Belo Horizonte')).toBeTruthy();
    });
  });

  it('deve exibir aviso e dados quando atualizando', async () => {
    vi.useFakeTimers();
    const { httpTesting, fixture } = await setup();
    const resposta: MunicipiosUfResponse = {
      statusProcessamento: 'atualizando',
      ultimoProcessamento: '2026-03-20T10:00:00Z',
      municipios: [
        { codigoIbge: '3106200', nome: 'Belo Horizonte', siglaEstado: 'MG', possuiAliquotas: true },
      ],
      semCertificado: false,
    };
    httpTesting.expectOne('/api/v1/estados/MG/municipios').flush(resposta);
    fixture.detectChanges();
    await waitFor(() => {
      expect(screen.getByText(/sendo atualizados/)).toBeTruthy();
      expect(screen.getByText('Belo Horizonte')).toBeTruthy();
    });
    fixture.componentInstance.ngOnDestroy();
    vi.useRealTimers();
  });

  it('deve exibir erro quando API falha', async () => {
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/estados/MG/municipios').error(new ProgressEvent('error'));
    fixture.detectChanges();
    await waitFor(() => {
      expect(screen.getByText(/Erro ao carregar municípios/)).toBeTruthy();
    });
  });

  it('deve limpar polling ao destruir componente', async () => {
    vi.useFakeTimers();
    const { httpTesting, fixture } = await setup();
    const resposta: MunicipiosUfResponse = {
      statusProcessamento: 'processamentoIniciado',
      ultimoProcessamento: null,
      municipios: [],
      semCertificado: false,
    };
    httpTesting.expectOne('/api/v1/estados/MG/municipios').flush(resposta);
    fixture.detectChanges();

    fixture.componentInstance.ngOnDestroy();

    // Após destroy, avançar timer não deve causar novas requisições
    vi.advanceTimersByTime(15_000);
    httpTesting.expectNone('/api/v1/estados/MG/municipios');
    vi.useRealTimers();
  });
});
