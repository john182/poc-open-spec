import { render, screen } from '@testing-library/angular';
import { EmptyStateComponent } from './empty-state.component';

describe('EmptyStateComponent', () => {
  it('deve exibir titulo e mensagem', async () => {
    await render(EmptyStateComponent, {
      inputs: { titulo: 'Nenhum resultado', mensagem: 'Tente ajustar os filtros' },
    });
    expect(screen.getByText('Nenhum resultado')).toBeTruthy();
    expect(screen.getByText('Tente ajustar os filtros')).toBeTruthy();
  });

  it('deve ter atributo data-cy', async () => {
    const { container } = await render(EmptyStateComponent, {
      inputs: { titulo: 'Vazio', mensagem: 'Sem dados' },
    });
    expect(container.querySelector('[data-cy="empty-state"]')).toBeTruthy();
  });

  it('deve exibir botao de acao quando acaoLabel fornecido', async () => {
    await render(EmptyStateComponent, {
      inputs: { titulo: 'Vazio', mensagem: 'Sem dados', acaoLabel: 'Recarregar' },
    });
    expect(screen.getByText('Recarregar')).toBeTruthy();
  });

  it('nao deve exibir botao quando acaoLabel ausente', async () => {
    await render(EmptyStateComponent, {
      inputs: { titulo: 'Vazio', mensagem: 'Sem dados' },
    });
    expect(screen.queryByRole('button')).toBeNull();
  });
});
