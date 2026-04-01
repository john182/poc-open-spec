import { render, screen } from '@testing-library/angular';
import { AppMenuComponent } from './app-menu.component';

describe('AppMenuComponent', () => {
  it('deve renderizar o menu', async () => {
    const { container } = await render(AppMenuComponent);
    expect(container.querySelector('[data-cy="app-menu"]')).toBeTruthy();
  });

  it('deve conter classe layout-menu', async () => {
    const { container } = await render(AppMenuComponent);
    expect(container.querySelector('.layout-menu')).toBeTruthy();
  });

  it('deve exibir label da secao Menu', async () => {
    await render(AppMenuComponent);
    expect(screen.getByText('Menu')).toBeTruthy();
  });

  it('deve exibir item Consulta de Aliquotas', async () => {
    await render(AppMenuComponent);
    expect(screen.getByText('Consulta de Alíquotas')).toBeTruthy();
  });

  it('deve usar classes do Sakai para root menuitem', async () => {
    const { container } = await render(AppMenuComponent);
    expect(container.querySelector('.layout-root-menuitem')).toBeTruthy();
    expect(container.querySelector('.layout-menuitem-root-text')).toBeTruthy();
  });

  it('deve usar classe layout-menuitem-icon no icone', async () => {
    const { container } = await render(AppMenuComponent);
    expect(container.querySelector('.layout-menuitem-icon')).toBeTruthy();
  });

  it('deve usar classe layout-menuitem-text no label', async () => {
    const { container } = await render(AppMenuComponent);
    expect(container.querySelector('.layout-menuitem-text')).toBeTruthy();
  });
});
