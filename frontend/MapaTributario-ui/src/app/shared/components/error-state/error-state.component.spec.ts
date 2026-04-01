import { render, screen } from '@testing-library/angular';
import { ErrorStateComponent } from './error-state.component';

describe('ErrorStateComponent', () => {
  it('deve exibir titulo padrao e mensagem', async () => {
    await render(ErrorStateComponent, {
      inputs: { mensagem: 'Falha na conexao' },
    });
    expect(screen.getByText('Erro ao carregar dados')).toBeTruthy();
    expect(screen.getByText('Falha na conexao')).toBeTruthy();
  });

  it('deve ter atributo data-cy', async () => {
    const { container } = await render(ErrorStateComponent, {
      inputs: { mensagem: 'Erro' },
    });
    expect(container.querySelector('[data-cy="error-state"]')).toBeTruthy();
  });

  it('deve exibir botao de retry', async () => {
    await render(ErrorStateComponent, {
      inputs: { mensagem: 'Erro' },
    });
    expect(screen.getByText('Tentar novamente')).toBeTruthy();
  });

  it('deve aceitar titulo customizado', async () => {
    await render(ErrorStateComponent, {
      inputs: { titulo: 'Ops!', mensagem: 'Algo deu errado' },
    });
    expect(screen.getByText('Ops!')).toBeTruthy();
  });
});
