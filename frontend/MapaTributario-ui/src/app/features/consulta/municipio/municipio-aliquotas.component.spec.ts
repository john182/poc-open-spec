import { render, screen, waitFor } from '@testing-library/angular';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { MunicipioAliquotasComponent } from './municipio-aliquotas.component';

describe('MunicipioAliquotasComponent', () => {
  async function setup(codigoIbge = '3106200', uf = 'MG', nome = 'Belo Horizonte') {
    const result = await render(MunicipioAliquotasComponent, {
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideAnimationsAsync(),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: { get: () => codigoIbge },
              queryParamMap: { get: (chave: string) => chave === 'uf' ? uf : chave === 'nome' ? nome : null },
            },
          },
        },
      ],
    });
    const httpTesting = TestBed.inject(HttpTestingController);
    return { ...result, httpTesting };
  }

  const respostaPaginada = (items: any[] = [], totalItens = 0, totalPaginas = 0) => ({
    items,
    pagina: 1,
    tamanhoPagina: 20,
    totalItens,
    totalPaginas,
  });

  const aliquotaExemplo = {
    codigoServico: '010101001',
    codigoServicoFormatado: '01.01.01.001',
    descricaoServico: 'Analise e desenvolvimento',
    aliquota: 2.0,
    competencia: '2026-03-01',
  };

  const aliquotaExemplo2 = {
    codigoServico: '070201001',
    codigoServicoFormatado: '07.02.01.001',
    descricaoServico: 'Servicos de limpeza',
    aliquota: 5.0,
    competencia: '2026-02-01',
  };

  const detalheAliquotaMock = {
    codigoServico: '010101001',
    codigoServicoFormatado: '01.01.01.001',
    descricaoServico: 'Analise e desenvolvimento',
    aliquota: 2.0,
    competencia: '2026-03-01',
    codigoMunicipio: '3106200',
    nomeMunicipio: 'Belo Horizonte',
    coletadoEm: '2026-03-15T14:00:00Z',
  };

  it('deve exibir tabela com loading enquanto carrega', async () => {
    const { container } = await setup();
    const tabela = container.querySelector('[data-cy="tabela-aliquotas"]');
    expect(tabela).toBeTruthy();
  });

  it('deve renderizar tabela apos carregar aliquotas', async () => {
    const { container, httpTesting, fixture } = await setup();
    httpTesting.expectOne(r => r.url === '/api/v1/municipios/3106200/aliquotas').flush(
      respostaPaginada([aliquotaExemplo], 1, 1)
    );
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(container.querySelector('app-error-state')).toBeNull();
  });

  it('deve exibir mensagem vazia quando nao ha aliquotas', async () => {
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne(r => r.url === '/api/v1/municipios/3106200/aliquotas').flush(
      respostaPaginada()
    );
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

  it('Given_DadosMunicipio_Should_ExibirTituloCorreto', async () => {
    // Arrange & Act
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne(r => r.url === '/api/v1/municipios/3106200/aliquotas').flush(
      respostaPaginada([aliquotaExemplo], 1, 1)
    );
    fixture.detectChanges();

    // Assert
    const componente = fixture.componentInstance;
    expect(componente.tituloMunicipio()).toBe('Belo Horizonte');
    expect(componente.codigoIbge()).toBe('3106200');
    expect(componente.uf()).toBe('MG');
  });

  it('Given_SemNomeMunicipio_Should_ExibirTituloComCodigo', async () => {
    // Arrange
    const { httpTesting, fixture } = await setup('9999999', 'XX', '');
    httpTesting.expectOne(r => r.url === '/api/v1/municipios/9999999/aliquotas').flush(
      respostaPaginada()
    );
    fixture.detectChanges();

    // Assert
    const componente = fixture.componentInstance;
    expect(componente.tituloMunicipio()).toBe('Município 9999999');
  });

  it('Given_AplicarFiltros_Should_RecarregarDadosComFiltros', async () => {
    // Arrange
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne(r => r.url === '/api/v1/municipios/3106200/aliquotas').flush(
      respostaPaginada([aliquotaExemplo], 1, 1)
    );
    fixture.detectChanges();

    const componente = fixture.componentInstance;
    componente.filtroDescricao.set('desenvolvimento');

    // Act
    componente.aplicarFiltros();

    // Assert
    const reqFiltro = httpTesting.expectOne(r => r.url === '/api/v1/municipios/3106200/aliquotas');
    expect(reqFiltro.request.params.get('descricao')).toBe('desenvolvimento');
    reqFiltro.flush(respostaPaginada([aliquotaExemplo], 1, 1));
    fixture.detectChanges();

    expect(componente.carregando()).toBe(false);
  });

  it('Given_LimparFiltros_Should_ResetarFiltrosERecarregar', async () => {
    // Arrange
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne(r => r.url === '/api/v1/municipios/3106200/aliquotas').flush(
      respostaPaginada([aliquotaExemplo], 1, 1)
    );
    fixture.detectChanges();

    const componente = fixture.componentInstance;
    componente.filtroDescricao.set('teste');
    componente.filtroAliquotaMin.set(1);
    componente.filtroAliquotaMax.set(5);

    // Act
    componente.limparFiltros();

    // Assert
    expect(componente.filtroDescricao()).toBe('');
    expect(componente.filtroAliquotaMin()).toBeNull();
    expect(componente.filtroAliquotaMax()).toBeNull();
    expect(componente.filtroCodigoServico()).toBe('');
    expect(componente.filtroCompetencia()).toBeNull();

    const reqLimpar = httpTesting.expectOne(r => r.url === '/api/v1/municipios/3106200/aliquotas');
    reqLimpar.flush(respostaPaginada([aliquotaExemplo, aliquotaExemplo2], 2, 1));
    fixture.detectChanges();
  });

  it('Given_VerDetalhe_Should_AbrirDialogComDadosDoDetalhe', async () => {
    // Arrange
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne(r => r.url === '/api/v1/municipios/3106200/aliquotas').flush(
      respostaPaginada([aliquotaExemplo], 1, 1)
    );
    fixture.detectChanges();

    const componente = fixture.componentInstance;

    // Act
    componente.verDetalhe(aliquotaExemplo as any);

    // Assert — loading + dialog visível
    expect(componente.detalheVisivel()).toBe(true);
    expect(componente.detalheCarregando()).toBe(true);

    httpTesting.expectOne('/api/v1/municipios/3106200/aliquotas/010101001').flush(detalheAliquotaMock);
    fixture.detectChanges();

    expect(componente.detalheCarregando()).toBe(false);
    expect(componente.detalhe()).toEqual(detalheAliquotaMock);
  });

  it('Given_FalhaNoDetalhe_Should_FecharDialogEPararLoading', async () => {
    // Arrange
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne(r => r.url === '/api/v1/municipios/3106200/aliquotas').flush(
      respostaPaginada([aliquotaExemplo], 1, 1)
    );
    fixture.detectChanges();

    const componente = fixture.componentInstance;

    // Act
    componente.verDetalhe(aliquotaExemplo as any);
    httpTesting.expectOne('/api/v1/municipios/3106200/aliquotas/010101001').error(new ProgressEvent('error'));

    // Assert
    expect(componente.detalheCarregando()).toBe(false);
    expect(componente.detalheVisivel()).toBe(false);
  });

  it('Given_TentarNovamente_Should_RecarregarDados', async () => {
    // Arrange
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne(r => r.url === '/api/v1/municipios/3106200/aliquotas').error(new ProgressEvent('error'));
    fixture.detectChanges();

    await waitFor(() => {
      expect(screen.getByText(/Erro ao carregar alíquotas/)).toBeTruthy();
    });

    // Act
    fixture.componentInstance.tentarNovamente();

    // Assert
    const reqRetry = httpTesting.expectOne(r => r.url === '/api/v1/municipios/3106200/aliquotas');
    reqRetry.flush(respostaPaginada([aliquotaExemplo], 1, 1));
    fixture.detectChanges();

    expect(fixture.componentInstance.erro()).toBe('');
    expect(fixture.componentInstance.carregando()).toBe(false);
  });

  it('Given_FiltroComCompetencia_Should_EnviarCompetenciaFormatada', async () => {
    // Arrange
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne(r => r.url === '/api/v1/municipios/3106200/aliquotas').flush(
      respostaPaginada([aliquotaExemplo], 1, 1)
    );
    fixture.detectChanges();

    const componente = fixture.componentInstance;
    componente.filtroCompetencia.set(new Date(2026, 2, 1)); // março 2026

    // Act
    componente.aplicarFiltros();

    // Assert
    const reqCompetencia = httpTesting.expectOne(r => r.url === '/api/v1/municipios/3106200/aliquotas');
    expect(reqCompetencia.request.params.get('competencia')).toBe('2026-03');
    reqCompetencia.flush(respostaPaginada([aliquotaExemplo], 1, 1));
  });

  it('Given_FiltroComAliquotaMinMax_Should_EnviarParametros', async () => {
    // Arrange
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne(r => r.url === '/api/v1/municipios/3106200/aliquotas').flush(
      respostaPaginada([aliquotaExemplo], 1, 1)
    );
    fixture.detectChanges();

    const componente = fixture.componentInstance;
    componente.filtroAliquotaMin.set(2);
    componente.filtroAliquotaMax.set(5);

    // Act
    componente.aplicarFiltros();

    // Assert
    const req = httpTesting.expectOne(r => r.url === '/api/v1/municipios/3106200/aliquotas');
    expect(req.request.params.get('aliquotaMin')).toBe('2');
    expect(req.request.params.get('aliquotaMax')).toBe('5');
    req.flush(respostaPaginada([aliquotaExemplo], 1, 1));
  });

  it('Given_Migalhas_Should_ConterUfNoPath', async () => {
    // Arrange
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne(r => r.url === '/api/v1/municipios/3106200/aliquotas').flush(
      respostaPaginada([], 0, 0)
    );
    fixture.detectChanges();

    // Assert
    const migalhas = fixture.componentInstance.migalhas();
    expect(migalhas.length).toBe(3);
    expect(migalhas[0].label).toBe('Consulta');
    expect(migalhas[1].label).toBe('MG');
    expect(migalhas[1].routerLink).toBe('/consulta/estado/MG');
    expect(migalhas[2].label).toBe('Belo Horizonte');
  });

  it('Given_PesquisarPaginada_Should_EnviarPaginaCorreta', async () => {
    // Arrange
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne(r => r.url === '/api/v1/municipios/3106200/aliquotas').flush(
      respostaPaginada([aliquotaExemplo], 1, 1)
    );
    fixture.detectChanges();

    const componente = fixture.componentInstance;

    // Act — simular evento de lazy load de segunda página
    componente.pesquisarPaginada({ first: 20, rows: 20 } as any);

    // Assert
    const req = httpTesting.expectOne(r => r.url === '/api/v1/municipios/3106200/aliquotas');
    expect(req.request.params.get('pagina')).toBe('1');
    expect(req.request.params.get('tamanhoPagina')).toBe('20');
    req.flush(respostaPaginada([aliquotaExemplo2], 21, 2));
    fixture.detectChanges();

    expect(componente.pagina().totalElementos).toBe(21);
    expect(componente.pagina().totalPaginas).toBe(2);
  });
});
