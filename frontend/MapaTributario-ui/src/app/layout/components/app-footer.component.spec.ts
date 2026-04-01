import { render, screen } from '@testing-library/angular';
import { AppFooterComponent } from './app-footer.component';

describe('AppFooterComponent', () => {
  it('deve renderizar o footer', async () => {
    const { container } = await render(AppFooterComponent);
    expect(container.querySelector('[data-cy="app-footer"]')).toBeTruthy();
  });

  it('deve conter classe layout-footer', async () => {
    const { container } = await render(AppFooterComponent);
    expect(container.querySelector('.layout-footer')).toBeTruthy();
  });

  it('deve exibir texto de copyright', async () => {
    await render(AppFooterComponent);
    const ano = new Date().getFullYear();
    expect(screen.getByText(`Mapa Tributário © ${ano}`)).toBeTruthy();
  });
});
