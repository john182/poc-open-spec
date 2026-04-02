import { render, screen, waitFor } from '@testing-library/angular';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { CrawlerConfiguracaoComponent } from './crawler-configuracao.component';

describe('CrawlerConfiguracaoComponent', () => {
  async function setup() {
    const result = await render(CrawlerConfiguracaoComponent, {
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
    const httpTesting = TestBed.inject(HttpTestingController);
    return { ...result, httpTesting };
  }

  const configuracaoMock = {
    id: '1',
    cronSchedule: '0 2 * * *',
    limiteRequisicoesPorSegundo: 15,
    orcamentoDiario: 50000,
    tamanheLoteCertificado: 200,
    pausaLoteSegundos: 5,
    tamanheLoteMongo: 50,
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
    atualizadoEm: '2026-03-01T12:00:00Z',
  };

  it('deve exibir loading inicialmente', async () => {
    const { container } = await setup();
    expect(container.querySelector('app-loading-spinner')).toBeTruthy();
  });

  it('deve exibir formulario apos carregar configuracao', async () => {
    const { container, httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/configuracao').flush(configuracaoMock);
    fixture.detectChanges();
    await waitFor(() => {
      expect(container.querySelector('[data-cy="secao-agendamento"]')).toBeTruthy();
      expect(container.querySelector('[data-cy="secao-controle-execucao"]')).toBeTruthy();
      expect(container.querySelector('[data-cy="secao-limites-protecao"]')).toBeTruthy();
      expect(container.querySelector('[data-cy="secao-exploracao"]')).toBeTruthy();
      expect(container.querySelector('[data-cy="secao-circuit-breaker"]')).toBeTruthy();
    });
  });

  it('deve exibir botoes de salvar e restaurar padrao', async () => {
    const { container, httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/configuracao').flush(configuracaoMock);
    fixture.detectChanges();
    await waitFor(() => {
      expect(container.querySelector('[data-cy="btn-salvar"]')).toBeTruthy();
      expect(container.querySelector('[data-cy="btn-restaurar-padrao"]')).toBeTruthy();
    });
  });

  it('deve exibir erro quando API falha', async () => {
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/configuracao').error(new ProgressEvent('error'));
    fixture.detectChanges();
    await waitFor(() => {
      expect(screen.getByText(/Erro ao carregar configuração do crawler/)).toBeTruthy();
    });
  });

  it('deve exibir informacoes de criacao e atualizacao', async () => {
    const { container, httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/configuracao').flush(configuracaoMock);
    fixture.detectChanges();
    await waitFor(() => {
      expect(container.querySelector('[data-cy="info-configuracao"]')).toBeTruthy();
    });
  });

  it('deve exibir mensagem de sucesso ao salvar', async () => {
    const { container, httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/configuracao').flush(configuracaoMock);
    fixture.detectChanges();

    await waitFor(() => {
      expect(container.querySelector('[data-cy="btn-salvar"]')).toBeTruthy();
    });

    fixture.componentInstance.salvar();
    fixture.detectChanges();

    const respostaAtualizada = { ...configuracaoMock, atualizadoEm: '2026-03-02T10:00:00Z' };
    httpTesting.expectOne('/api/v1/crawler/configuracao').flush(respostaAtualizada);
    fixture.detectChanges();

    await waitFor(() => {
      expect(container.querySelector('[data-cy="msg-sucesso"]')).toBeTruthy();
      expect(screen.getByText(/Configuração salva com sucesso/)).toBeTruthy();
    });
  });

  it('deve exibir mensagem de erro ao falhar ao salvar', async () => {
    const { container, httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/configuracao').flush(configuracaoMock);
    fixture.detectChanges();

    await waitFor(() => {
      expect(container.querySelector('[data-cy="btn-salvar"]')).toBeTruthy();
    });

    fixture.componentInstance.salvar();
    fixture.detectChanges();

    httpTesting.expectOne('/api/v1/crawler/configuracao').flush(
      { erro: 'Erro de validação', detalhes: ['Campo inválido'] },
      { status: 422, statusText: 'Unprocessable Entity' },
    );
    fixture.detectChanges();

    await waitFor(() => {
      expect(container.querySelector('[data-cy="msg-erro"]')).toBeTruthy();
    });
  });

  it('deve restaurar valores padrao ao clicar em restaurar', async () => {
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/configuracao').flush({
      ...configuracaoMock,
      cronSchedule: '0 5 * * *',
      maxTentativas: 10,
    });
    fixture.detectChanges();

    fixture.componentInstance.restaurarPadrao();
    fixture.detectChanges();

    expect(fixture.componentInstance.cronSchedule()).toBe('0 2 * * *');
    expect(fixture.componentInstance.maxTentativas()).toBe(3);
    expect(fixture.componentInstance.limiteRequisicoesPorSegundo()).toBe(15);
    expect(fixture.componentInstance.codigosSondagemTexto()).toBe('01.01.01, 07.02.01, 14.01.01, 17.01.01, 25.01.01');
  });
});
