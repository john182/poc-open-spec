import { render, screen } from '@testing-library/angular';
import { AccessDeniedComponent } from './access-denied.component';

describe('AccessDeniedComponent', () => {
  it('deve renderizar pagina de acesso negado', async () => {
    const { container } = await render(AccessDeniedComponent);
    expect(container.querySelector('[data-cy="access-denied-page"]')).toBeTruthy();
  });

  it('deve exibir titulo Acesso Negado', async () => {
    await render(AccessDeniedComponent);
    expect(screen.getByText('Acesso Negado')).toBeTruthy();
  });

  it('deve exibir mensagem de permissao', async () => {
    await render(AccessDeniedComponent);
    expect(screen.getByText('Você não tem permissão para acessar esta página.')).toBeTruthy();
  });

  it('deve ter icone de cadeado', async () => {
    const { container } = await render(AccessDeniedComponent);
    expect(container.querySelector('.pi-lock')).toBeTruthy();
  });

  it('deve ter botao Voltar ao inicio', async () => {
    await render(AccessDeniedComponent);
    expect(screen.getByText('Voltar ao início')).toBeTruthy();
  });

  it('deve ter host com display contents', async () => {
    const { fixture } = await render(AccessDeniedComponent);
    const hostEl = fixture.nativeElement as HTMLElement;
    expect(hostEl.style.display).toBe('contents');
  });
});
