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

  it('deve exibir loading inicialmente', async () => {
    const { container } = await setup();
    expect(container.querySelector('app-loading-spinner')).toBeTruthy();
  });

  it('deve exibir status ativo quando certificado esta configurado', async () => {
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/certificado').flush({
      hasCertificate: true,
      uploadedAt: '2026-03-01T10:00:00Z',
    });
    fixture.detectChanges();
    await waitFor(() => {
      expect(screen.getByText('Ativo')).toBeTruthy();
      expect(screen.getByText(/Enviado em/)).toBeTruthy();
    });
  });

  it('deve exibir status nao configurado quando sem certificado', async () => {
    const { httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/certificado').flush({
      hasCertificate: false,
      uploadedAt: null,
    });
    fixture.detectChanges();
    await waitFor(() => {
      expect(screen.getByText('Não configurado')).toBeTruthy();
    });
  });

  it('deve exibir formulario de upload', async () => {
    const { container, httpTesting, fixture } = await setup();
    httpTesting.expectOne('/api/v1/crawler/certificado').flush({
      hasCertificate: false,
      uploadedAt: null,
    });
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
});
