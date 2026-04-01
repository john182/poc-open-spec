import { render, screen } from '@testing-library/angular';
import { LoginComponent } from './login.component';

describe('LoginComponent', () => {
  it('deve renderizar pagina de login', async () => {
    const { container } = await render(LoginComponent);
    expect(container.querySelector('[data-cy="login-page"]')).toBeTruthy();
  });

  it('deve exibir titulo Mapa Tributario', async () => {
    await render(LoginComponent);
    expect(screen.getByText('Mapa Tributário')).toBeTruthy();
  });

  it('deve exibir subtitulo', async () => {
    await render(LoginComponent);
    expect(screen.getByText('Entre para continuar')).toBeTruthy();
  });

  it('deve ter campo de email', async () => {
    await render(LoginComponent);
    expect(screen.getByLabelText('Email')).toBeTruthy();
  });

  it('deve ter campo de senha', async () => {
    const { container } = await render(LoginComponent);
    expect(container.querySelector('p-password#senha')).toBeTruthy();
  });

  it('deve ter checkbox lembrar-me', async () => {
    const { container } = await render(LoginComponent);
    expect(container.querySelector('p-checkbox#lembrar')).toBeTruthy();
  });

  it('deve ter botao Entrar', async () => {
    await render(LoginComponent);
    expect(screen.getByText('Entrar')).toBeTruthy();
  });

  it('deve ter link para criar conta', async () => {
    await render(LoginComponent);
    expect(screen.getByText('Criar conta')).toBeTruthy();
  });

  it('deve ter host com display contents', async () => {
    const { fixture } = await render(LoginComponent);
    const hostEl = fixture.nativeElement as HTMLElement;
    expect(hostEl.style.display).toBe('contents');
  });
});
