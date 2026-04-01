import { render, screen } from '@testing-library/angular';
import { ConsultaPlaceholderComponent } from './consulta-placeholder.component';

describe('ConsultaPlaceholderComponent', () => {
  it('deve renderizar pagina de consulta', async () => {
    const { container } = await render(ConsultaPlaceholderComponent);
    expect(container.querySelector('[data-cy="consulta-page"]')).toBeTruthy();
  });

  it('deve exibir titulo Consulta de Aliquotas', async () => {
    await render(ConsultaPlaceholderComponent);
    expect(screen.getByText('Consulta de Alíquotas')).toBeTruthy();
  });

  it('deve indicar que sera implementada na PBI 8', async () => {
    await render(ConsultaPlaceholderComponent);
    expect(screen.getByText(/PBI #8/)).toBeTruthy();
  });
});
