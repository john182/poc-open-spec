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

  it('deve ter botao de menu toggle', async () => {
    const { container } = await render(AppTopbarComponent);
    expect(container.querySelector('.layout-menu-button')).toBeTruthy();
  });

  it('deve ter botao de dark mode', async () => {
    const { container } = await render(AppTopbarComponent);
    expect(container.querySelector('.pi-moon') || container.querySelector('.pi-sun')).toBeTruthy();
  });
});
