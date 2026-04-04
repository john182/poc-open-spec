import { render, screen, waitFor } from '@testing-library/angular';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { CrawlerCertificadoComponent } from './crawler-certificado.component';

describe('CrawlerCertificadoComponent', () => {
  async function setup() {
    const result = await render(CrawlerCertificadoComponent, {
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
    const httpTesting = TestBed.inject(HttpTestingController);
    return { ...result, httpTesting };
  }

  const certificadoAtivoMock = {
    hasCertificate: true,
    uploadedAt: '2026-03-01T10:00:00Z',
    thumbprint: 'AB12CD34EF56',
    subject: 'CN=Empresa Teste LTDA',
    validoAte: '2027-06-15T12:00:00Z',
  };

  const certificadoInativoMock = {
    hasCertificate: false,
    uploadedAt: null,
    thumbprint: null,
    subject: null,
    validoAte: null,
  };

  function criarMockProximoVencimento(): typeof certificadoAtivoMock {
    const daquiA15Dias = new Date();
    daquiA15Dias.setDate(daquiA15Dias.getDate() + 15);
    return {
      ...certificadoAtivoMock,
      validoAte: daquiA15Dias.toISOString(),
    };
  }

  function criarMockVencido(): typeof certificadoAtivoMock {
    return {
      ...certificadoAtivoMock,
      validoAte: '2025-01-01T00:00:00Z',
    };
  }

  it('deve exibir loading inicialmente', async () => {
    const { container } = await setup();
    expect(container.querySelector('app-loading-spinner')).toBeTruthy();
  });

  it('deve exibir status ativo quando certificado esta configurado', async () => {
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/certificado').flush(certificadoAtivoMock);
    fixture.detectChanges();
    await waitFor(() => {
      expect(screen.getByText('Ativo')).toBeTruthy();
      expect(screen.getByText(/Enviado em/)).toBeTruthy();
    });
  });

  it('deve exibir status nao configurado quando sem certificado', async () => {
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/certificado').flush(certificadoInativoMock);
    fixture.detectChanges();
    await waitFor(() => {
      expect(screen.getByText('Não configurado')).toBeTruthy();
    });
  });

  it('deve exibir formulario de upload', async () => {
    const { container, httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/certificado').flush(certificadoInativoMock);
    fixture.detectChanges();
    await waitFor(() => {
      expect(container.querySelector('[data-cy="certificado-upload"]')).toBeTruthy();
      expect(container.querySelector('[data-cy="input-senha-certificado"]')).toBeTruthy();
    });
  });

  it('deve exibir erro quando API falha', async () => {
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/certificado').error(new ProgressEvent('error'));
    fixture.detectChanges();
    await waitFor(() => {
      expect(screen.getByText(/Erro ao carregar status do certificado/)).toBeTruthy();
    });
  });

  it('Given_CertificadoAtivo_Should_ExibirBotaoRemover', async () => {
    // Arrange
    const { container, httpTesting, fixture } = await setup();

    // Act
    httpTesting.expectOne('/api/v1/crawler/certificado').flush(certificadoAtivoMock);
    fixture.detectChanges();

    // Assert
    await waitFor(() => {
      expect(container.querySelector('[data-cy="btn-remover-certificado"]')).toBeTruthy();
    });
  });

  it('Given_CertificadoInativo_Should_NaoExibirBotaoRemover', async () => {
    // Arrange
    const { container, httpTesting, fixture } = await setup();

    // Act
    httpTesting.expectOne('/api/v1/crawler/certificado').flush(certificadoInativoMock);
    fixture.detectChanges();

    // Assert
    await waitFor(() => {
      expect(container.querySelector('[data-cy="btn-remover-certificado"]')).toBeNull();
    });
  });

  it('Given_ArquivoESenhaPreenchidos_Should_EnviarCertificadoComSucesso', async () => {
    // Arrange
    const { container, httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/certificado').flush(certificadoInativoMock);
    fixture.detectChanges();

    const componente = fixture.componentInstance;
    const arquivoFake = new File(['conteudo'], 'cert.pfx', { type: 'application/x-pkcs12' });
    componente.arquivoSelecionado.set(arquivoFake);
    componente.senhaCertificado.set('senha123');

    // Act
    componente.enviarCertificado();

    // Assert
    const reqUpload = httpTesting.expectOne('/api/v1/crawler/certificado');
    expect(reqUpload.request.method).toBe('POST');
    reqUpload.flush({ mensagem: 'Certificado enviado com sucesso' });

    // Após sucesso, recarrega status
    httpTesting.expectOne('/api/v1/crawler/certificado').flush(certificadoAtivoMock);
    fixture.detectChanges();

    await waitFor(() => {
      expect(container.querySelector('[data-cy="msg-upload-sucesso"]')).toBeTruthy();
      expect(screen.getByText(/Certificado enviado com sucesso/)).toBeTruthy();
    });

    expect(componente.arquivoSelecionado()).toBeNull();
    expect(componente.senhaCertificado()).toBe('');
    expect(componente.enviando()).toBe(false);
  });

  it('Given_FalhaNoUpload_Should_ExibirMensagemDeErro', async () => {
    // Arrange
    const { container, httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/certificado').flush(certificadoInativoMock);
    fixture.detectChanges();

    const componente = fixture.componentInstance;
    const arquivoFake = new File(['conteudo'], 'cert.pfx', { type: 'application/x-pkcs12' });
    componente.arquivoSelecionado.set(arquivoFake);
    componente.senhaCertificado.set('senha123');

    // Act
    componente.enviarCertificado();

    httpTesting.expectOne('/api/v1/crawler/certificado').flush(
      { erro: 'Senha do certificado inválida' },
      { status: 400, statusText: 'Bad Request' },
    );
    fixture.detectChanges();

    // Assert
    await waitFor(() => {
      expect(container.querySelector('[data-cy="msg-upload-erro"]')).toBeTruthy();
      expect(screen.getByText('Senha do certificado inválida')).toBeTruthy();
    });
    expect(componente.enviando()).toBe(false);
  });

  it('Given_FalhaNoUploadSemDetalhe_Should_ExibirMensagemPadrao', async () => {
    // Arrange
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/certificado').flush(certificadoInativoMock);
    fixture.detectChanges();

    const componente = fixture.componentInstance;
    const arquivoFake = new File(['conteudo'], 'cert.pfx', { type: 'application/x-pkcs12' });
    componente.arquivoSelecionado.set(arquivoFake);
    componente.senhaCertificado.set('senha123');

    // Act
    componente.enviarCertificado();

    httpTesting.expectOne('/api/v1/crawler/certificado').error(new ProgressEvent('error'));
    fixture.detectChanges();

    // Assert
    expect(componente.erroUpload()).toBe('Erro ao enviar certificado.');
    expect(componente.enviando()).toBe(false);
  });

  it('Given_SemArquivoOuSenha_Should_NaoEnviarRequisicao', async () => {
    // Arrange
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/certificado').flush(certificadoInativoMock);
    fixture.detectChanges();

    const componente = fixture.componentInstance;

    // Act — sem arquivo e sem senha
    componente.enviarCertificado();

    // Assert
    httpTesting.expectNone('/api/v1/crawler/certificado');
    expect(componente.enviando()).toBe(false);
  });

  it('Given_RemocaoComSucesso_Should_RecarregarStatus', async () => {
    // Arrange
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/certificado').flush(certificadoAtivoMock);
    fixture.detectChanges();

    const componente = fixture.componentInstance;

    // Act
    componente.removerCertificado();
    expect(componente.removendo()).toBe(true);

    const reqRemover = httpTesting.expectOne('/api/v1/crawler/certificado');
    expect(reqRemover.request.method).toBe('DELETE');
    reqRemover.flush({ mensagem: 'Certificado removido' });

    // Recarrega status
    httpTesting.expectOne('/api/v1/crawler/certificado').flush(certificadoInativoMock);
    fixture.detectChanges();

    // Assert
    expect(componente.removendo()).toBe(false);
    await waitFor(() => {
      expect(screen.getByText('Não configurado')).toBeTruthy();
    });
  });

  it('Given_FalhaRemocao_Should_PararRemovendo', async () => {
    // Arrange
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/certificado').flush(certificadoAtivoMock);
    fixture.detectChanges();

    const componente = fixture.componentInstance;

    // Act
    componente.removerCertificado();
    httpTesting.expectOne('/api/v1/crawler/certificado').error(new ProgressEvent('error'));

    // Assert
    expect(componente.removendo()).toBe(false);
  });

  it('Given_ArquivoSelecionado_Should_ExibirNomeDoArquivo', async () => {
    // Arrange
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/certificado').flush(certificadoInativoMock);
    fixture.detectChanges();

    const componente = fixture.componentInstance;

    // Act
    const arquivoFake = new File(['conteudo'], 'meu-certificado.pfx', { type: 'application/x-pkcs12' });
    componente.onArquivoSelecionado({ files: [arquivoFake] });
    fixture.detectChanges();

    // Assert
    await waitFor(() => {
      expect(screen.getByText('meu-certificado.pfx')).toBeTruthy();
    });
    expect(componente.arquivoSelecionado()).toBe(arquivoFake);
  });

  it('Given_EventoSemArquivo_Should_NaoAlterarArquivoSelecionado', async () => {
    // Arrange
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/certificado').flush(certificadoInativoMock);
    fixture.detectChanges();

    const componente = fixture.componentInstance;

    // Act
    componente.onArquivoSelecionado({ files: [] });

    // Assert
    expect(componente.arquivoSelecionado()).toBeNull();
  });

  it('Given_ErroNaTela_Should_RecarregarAoClicarTentarNovamente', async () => {
    // Arrange
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/certificado').error(new ProgressEvent('error'));
    fixture.detectChanges();

    await waitFor(() => {
      expect(screen.getByText(/Erro ao carregar status do certificado/)).toBeTruthy();
    });

    // Act
    fixture.componentInstance.tentarNovamente();

    // Assert
    const reqRetry = httpTesting.expectOne('/api/v1/crawler/certificado');
    reqRetry.flush(certificadoAtivoMock);
    fixture.detectChanges();

    await waitFor(() => {
      expect(screen.getByText('Ativo')).toBeTruthy();
    });
  });

  it('Given_CertificadoInativo_Should_ExibirMensagemNenhumCertificado', async () => {
    // Arrange & Act
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/certificado').flush(certificadoInativoMock);
    fixture.detectChanges();

    // Assert
    await waitFor(() => {
      expect(screen.getByText(/Nenhum certificado PFX carregado/)).toBeTruthy();
    });
  });

  it('Given_CertificadoAtivoComMetadados_Should_ExibirSubjectThumbprintValidade', async () => {
    // Arrange & Act
    const { container, httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/certificado').flush(certificadoAtivoMock);
    fixture.detectChanges();

    // Assert
    await waitFor(() => {
      const secaoMetadados = container.querySelector('[data-cy="metadados-certificado"]');
      expect(secaoMetadados).toBeTruthy();
      expect(container.querySelector('[data-cy="certificado-subject"]')?.textContent).toContain('CN=Empresa Teste LTDA');
      expect(container.querySelector('[data-cy="certificado-thumbprint"]')?.textContent).toContain('AB12CD34EF56');
      expect(container.querySelector('[data-cy="certificado-validade"]')?.textContent).toContain('15/06/2027');
    });
  });

  it('Given_CertificadoInativo_Should_NaoExibirMetadados', async () => {
    // Arrange & Act
    const { container, httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/certificado').flush(certificadoInativoMock);
    fixture.detectChanges();

    // Assert
    await waitFor(() => {
      expect(container.querySelector('[data-cy="metadados-certificado"]')).toBeNull();
    });
  });

  it('Given_CertificadoProximoDoVencimento_Should_ExibirAlertaProximoVencimento', async () => {
    // Arrange
    const mock = criarMockProximoVencimento();
    const { container, httpTesting, fixture } = await setup();

    // Act
    httpTesting.expectOne('/api/v1/crawler/certificado').flush(mock);
    fixture.detectChanges();

    // Assert
    await waitFor(() => {
      expect(container.querySelector('[data-cy="tag-proximo-vencimento"]')).toBeTruthy();
      expect(container.querySelector('[data-cy="alerta-certificado-proximo-vencimento"]')).toBeTruthy();
      expect(container.querySelector('[data-cy="alerta-certificado-proximo-vencimento"]')?.textContent).toContain('vence em');
    });
  });

  it('Given_CertificadoVencido_Should_ExibirAlertaVencido', async () => {
    // Arrange
    const mock = criarMockVencido();
    const { container, httpTesting, fixture } = await setup();

    // Act
    httpTesting.expectOne('/api/v1/crawler/certificado').flush(mock);
    fixture.detectChanges();

    // Assert
    await waitFor(() => {
      expect(container.querySelector('[data-cy="tag-vencido"]')).toBeTruthy();
      expect(container.querySelector('[data-cy="alerta-certificado-vencido"]')).toBeTruthy();
      expect(container.querySelector('[data-cy="alerta-certificado-vencido"]')?.textContent).toContain('vencido');
    });
  });

  it('Given_CertificadoComValidadeLonga_Should_NaoExibirAlerta', async () => {
    // Arrange & Act
    const { container, httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/certificado').flush(certificadoAtivoMock);
    fixture.detectChanges();

    // Assert
    await waitFor(() => {
      expect(container.querySelector('[data-cy="alerta-certificado-vencido"]')).toBeNull();
      expect(container.querySelector('[data-cy="alerta-certificado-proximo-vencimento"]')).toBeNull();
      expect(screen.getByText('Ativo')).toBeTruthy();
    });
  });

  it('Given_CertificadoProximoDoVencimento_Should_ComputedSinaisCorretos', async () => {
    // Arrange
    const mock = criarMockProximoVencimento();
    const { httpTesting, fixture } = await setup();

    // Act
    httpTesting.expectOne('/api/v1/crawler/certificado').flush(mock);
    fixture.detectChanges();

    // Assert
    const componente = fixture.componentInstance;
    expect(componente.certificadoProximoDoVencimento()).toBe(true);
    expect(componente.certificadoVencido()).toBe(false);
    expect(componente.diasParaVencimento()).toBeLessThanOrEqual(30);
    expect(componente.diasParaVencimento()).toBeGreaterThan(0);
  });

  it('Given_CertificadoVencido_Should_ComputedSinaisCorretos', async () => {
    // Arrange
    const mock = criarMockVencido();
    const { httpTesting, fixture } = await setup();

    // Act
    httpTesting.expectOne('/api/v1/crawler/certificado').flush(mock);
    fixture.detectChanges();

    // Assert
    const componente = fixture.componentInstance;
    expect(componente.certificadoVencido()).toBe(true);
    expect(componente.certificadoProximoDoVencimento()).toBe(false);
    expect(componente.diasParaVencimento()).toBeLessThan(0);
  });

  it('Given_CertificadoSemValidoAte_Should_ComputedSinaisRetornarFalsoOuNull', async () => {
    // Arrange
    const mockSemValidade = { ...certificadoAtivoMock, validoAte: null };
    const { httpTesting, fixture } = await setup();

    // Act
    httpTesting.expectOne('/api/v1/crawler/certificado').flush(mockSemValidade);
    fixture.detectChanges();

    // Assert
    const componente = fixture.componentInstance;
    expect(componente.certificadoVencido()).toBe(false);
    expect(componente.certificadoProximoDoVencimento()).toBe(false);
    expect(componente.diasParaVencimento()).toBeNull();
  });
});
