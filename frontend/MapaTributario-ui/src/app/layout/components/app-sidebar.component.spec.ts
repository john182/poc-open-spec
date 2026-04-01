import { render } from '@testing-library/angular';
import { AppSidebarComponent } from './app-sidebar.component';

describe('AppSidebarComponent', () => {
  it('deve renderizar o sidebar', async () => {
    const { container } = await render(AppSidebarComponent);
    expect(container.querySelector('[data-cy="app-sidebar"]')).toBeTruthy();
  });

  it('deve conter classe layout-sidebar', async () => {
    const { container } = await render(AppSidebarComponent);
    expect(container.querySelector('.layout-sidebar')).toBeTruthy();
  });

  it('deve renderizar o menu dentro do sidebar', async () => {
    const { container } = await render(AppSidebarComponent);
    expect(container.querySelector('[data-cy="app-menu"]')).toBeTruthy();
  });
});
