import { render, screen } from '@testing-library/angular';
import { PageHeaderComponent } from './page-header.component';

describe('PageHeaderComponent', () => {
  it('deve renderizar o page header', async () => {
    const { container } = await render(PageHeaderComponent, {
      inputs: { titulo: 'Consulta' },
    });
    expect(container.querySelector('[data-cy="page-header"]')).toBeTruthy();
  });

  it('deve exibir titulo', async () => {
    await render(PageHeaderComponent, {
      inputs: { titulo: 'Consulta de Alíquotas' },
    });
    expect(screen.getByText('Consulta de Alíquotas')).toBeTruthy();
  });
});
