import { render, screen } from '@testing-library/angular';
import { SignupComponent } from './signup.component';

describe('SignupComponent', () => {
  it('deve renderizar pagina de signup', async () => {
    const { container } = await render(SignupComponent);
    expect(container.querySelector('[data-cy="signup-page"]')).toBeTruthy();
  });

  it('deve exibir titulo Criar Conta', async () => {
    const { container } = await render(SignupComponent);
    expect(container.querySelector('.text-3xl')?.textContent).toBe('Criar Conta');
  });

  it('deve ter campo de nome', async () => {
    await render(SignupComponent);
    expect(screen.getByLabelText('Nome')).toBeTruthy();
  });

  it('deve ter campo de email', async () => {
    await render(SignupComponent);
    expect(screen.getByLabelText('Email')).toBeTruthy();
  });

  it('deve ter campo de senha', async () => {
    const { container } = await render(SignupComponent);
    expect(container.querySelector('p-password#senha')).toBeTruthy();
  });

  it('deve ter campo de confirmar senha', async () => {
    const { container } = await render(SignupComponent);
    expect(container.querySelector('p-password#confirmar')).toBeTruthy();
  });

  it('deve ter botao Criar Conta', async () => {
    await render(SignupComponent);
    expect(screen.getByRole('button', { name: 'Criar Conta' })).toBeTruthy();
  });

  it('deve ter link para entrar', async () => {
    await render(SignupComponent);
    expect(screen.getByText('Entrar')).toBeTruthy();
  });

  it('deve ter host com display contents', async () => {
    const { fixture } = await render(SignupComponent);
    const hostEl = fixture.nativeElement as HTMLElement;
    expect(hostEl.style.display).toBe('contents');
  });
});
