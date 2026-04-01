import { render, screen } from '@testing-library/angular';
import { AppTopbarComponent } from './app-topbar.component';

describe('AppTopbarComponent', () => {
  it('deve renderizar o topbar', async () => {
    const { container } = await render(AppTopbarComponent);
    expect(container.querySelector('[data-cy="app-topbar"]')).toBeTruthy();
  });

  it('deve exibir nome da aplicacao', async () => {
    await render(AppTopbarComponent);
    expect(screen.getByText('Mapa Tributário')).toBeTruthy();
  });

  it('deve ter botao de menu toggle com acessibilidade', async () => {
    const { container } = await render(AppTopbarComponent);
    const btn = container.querySelector('.layout-menu-button') as HTMLButtonElement;
    expect(btn).toBeTruthy();
    expect(btn.getAttribute('type')).toBe('button');
    expect(btn.getAttribute('aria-label')).toBe('Alternar menu');
  });

  it('deve ter botao de dark mode com acessibilidade', async () => {
    const { container } = await render(AppTopbarComponent);
    const btn = container.querySelector('.layout-config-menu button') as HTMLButtonElement;
    expect(btn).toBeTruthy();
    expect(btn.getAttribute('type')).toBe('button');
    expect(btn.getAttribute('aria-label')).toBeTruthy();
    expect(btn.hasAttribute('aria-pressed')).toBe(true);
  });
});
