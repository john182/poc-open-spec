import { render, screen } from '@testing-library/angular';
import { LoadingSpinnerComponent } from './loading-spinner.component';

describe('LoadingSpinnerComponent', () => {
  it('deve renderizar o spinner', async () => {
    const { container } = await render(LoadingSpinnerComponent);
    expect(container.querySelector('[data-cy="loading-spinner"]')).toBeTruthy();
  });

  it('deve exibir mensagem quando fornecida', async () => {
    await render(LoadingSpinnerComponent, {
      inputs: { mensagem: 'Carregando dados...' },
    });
    expect(screen.getByText('Carregando dados...')).toBeTruthy();
  });

  it('nao deve exibir mensagem quando nao fornecida', async () => {
    await render(LoadingSpinnerComponent);
    expect(screen.queryByText('Carregando')).toBeNull();
  });
});
