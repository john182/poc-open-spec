import { render, screen } from '@testing-library/angular';
import { NotFoundComponent } from './not-found.component';

describe('NotFoundComponent', () => {
  it('deve renderizar pagina 404', async () => {
    const { container } = await render(NotFoundComponent);
    expect(container.querySelector('[data-cy="not-found-page"]')).toBeTruthy();
  });

  it('deve exibir codigo 404', async () => {
    await render(NotFoundComponent);
    expect(screen.getByText('404')).toBeTruthy();
  });

  it('deve exibir titulo Pagina nao encontrada', async () => {
    await render(NotFoundComponent);
    expect(screen.getByText('Página não encontrada')).toBeTruthy();
  });

  it('deve exibir mensagem descritiva', async () => {
    await render(NotFoundComponent);
    expect(screen.getByText('O recurso solicitado não está disponível.')).toBeTruthy();
  });

  it('deve ter botao Voltar ao inicio', async () => {
    await render(NotFoundComponent);
    expect(screen.getByText('Voltar ao início')).toBeTruthy();
  });

  it('deve ter host com display contents', async () => {
    const { fixture } = await render(NotFoundComponent);
    const hostEl = fixture.nativeElement as HTMLElement;
    expect(hostEl.style.display).toBe('contents');
  });
});
