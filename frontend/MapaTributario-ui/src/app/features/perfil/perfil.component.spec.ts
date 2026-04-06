import { render, screen, fireEvent } from '@testing-library/angular';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { PLATFORM_ID } from '@angular/core';
import { PerfilComponent } from './perfil.component';
import { AuthService } from '../../core/auth/auth.service';

function makeJwt(payload: Record<string, unknown>): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(JSON.stringify(payload));
  return `${header}.${body}.fake-signature`;
}

const validPayload = {
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier': 'user-123',
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress': 'teste@test.com',
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name': 'Nome Atualizado',
  exp: Math.floor(Date.now() / 1000) + 3600,
};

const perfilMock = { id: '123', nome: 'João Silva', email: 'joao@test.com' };

describe('PerfilComponent', () => {
  let httpTesting: HttpTestingController;

  async function setup() {
    const result = await render(PerfilComponent, {
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: 'auth/login', component: {} as any }]),
        { provide: PLATFORM_ID, useValue: 'browser' },
      ],
    });
    httpTesting = TestBed.inject(HttpTestingController);
    const authService = TestBed.inject(AuthService);
    return { ...result, authService };
  }

  afterEach(() => {
    httpTesting.verify();
    localStorage.clear();
    sessionStorage.clear();
  });

  it('deve exibir spinner enquanto carrega', async () => {
    const { container } = await setup();
    expect(container.querySelector('.pi-spinner')).toBeTruthy();

    // Finalizar a requisição pendente
    httpTesting.expectOne('/api/v1/perfil').flush(perfilMock);
  });

  it('deve carregar e exibir dados do perfil', async () => {
    const { container } = await setup();

    httpTesting.expectOne('/api/v1/perfil').flush(perfilMock);
    await new Promise(r => setTimeout(r, 0));

    const nomeInput = container.querySelector('[data-cy="perfil-nome"]') as HTMLInputElement;
    const emailInput = container.querySelector('[data-cy="perfil-email"]') as HTMLInputElement;

    expect(nomeInput).toBeTruthy();
    expect(nomeInput.value).toBe('João Silva');
    expect(emailInput).toBeTruthy();
    expect(emailInput.value).toBe('joao@test.com');
    expect(emailInput.disabled).toBe(true);
  });

  it('deve exibir mensagem de erro ao falhar ao carregar perfil', async () => {
    await setup();

    httpTesting.expectOne('/api/v1/perfil').flush(
      { erro: 'Erro' },
      { status: 500, statusText: 'Internal Server Error' },
    );
    await new Promise(r => setTimeout(r, 0));

    expect(screen.getByText('Erro ao carregar perfil. Tente novamente.')).toBeTruthy();
  });

  it('deve atualizar perfil com sucesso e atualizar token', async () => {
    localStorage.setItem('rememberMe', 'true');
    const { container, authService } = await setup();
    const espiao = vi.spyOn(authService, 'atualizarToken');

    httpTesting.expectOne('/api/v1/perfil').flush(perfilMock);
    await new Promise(r => setTimeout(r, 0));

    // Alterar nome
    const nomeInput = container.querySelector('[data-cy="perfil-nome"]') as HTMLInputElement;
    fireEvent.input(nomeInput, { target: { value: 'Nome Atualizado' } });

    // Submeter
    const form = container.querySelector('form') as HTMLFormElement;
    fireEvent.submit(form);

    const novoToken = makeJwt(validPayload);
    const respostaMock = { id: '123', nome: 'Nome Atualizado', email: 'joao@test.com', accessToken: novoToken };
    httpTesting.expectOne({ method: 'PUT', url: '/api/v1/perfil' }).flush(respostaMock);
    await new Promise(r => setTimeout(r, 0));

    expect(espiao).toHaveBeenCalledWith(novoToken);
    expect(screen.getByText('Perfil atualizado com sucesso!')).toBeTruthy();
  });

  it('deve exibir erro ao tentar alterar senha sem informar senha atual', async () => {
    const { container } = await setup();

    httpTesting.expectOne('/api/v1/perfil').flush(perfilMock);
    await new Promise(r => setTimeout(r, 0));

    // Preencher apenas nova senha e confirmação (sem senha atual)
    const novaSenhaInput = container.querySelector('[data-cy="perfil-nova-senha"] input') as HTMLInputElement;
    const confirmarInput = container.querySelector('[data-cy="perfil-confirmar-nova-senha"] input') as HTMLInputElement;
    if (novaSenhaInput) {
      fireEvent.input(novaSenhaInput, { target: { value: 'novaSenha123' } });
    }
    if (confirmarInput) {
      fireEvent.input(confirmarInput, { target: { value: 'novaSenha123' } });
    }

    const form = container.querySelector('form') as HTMLFormElement;
    fireEvent.submit(form);
    await new Promise(r => setTimeout(r, 0));

    expect(screen.getByText('Informe a senha atual para alterar a senha.')).toBeTruthy();
  });

  it('deve exibir erro quando senhas não coincidem', async () => {
    const { container } = await setup();

    httpTesting.expectOne('/api/v1/perfil').flush(perfilMock);
    await new Promise(r => setTimeout(r, 0));

    // Preencher senha atual, nova senha e confirmação diferente
    const senhaAtualInput = container.querySelector('[data-cy="perfil-senha-atual"] input') as HTMLInputElement;
    const novaSenhaInput = container.querySelector('[data-cy="perfil-nova-senha"] input') as HTMLInputElement;
    const confirmarInput = container.querySelector('[data-cy="perfil-confirmar-nova-senha"] input') as HTMLInputElement;
    if (senhaAtualInput) {
      fireEvent.input(senhaAtualInput, { target: { value: '12345678' } });
    }
    if (novaSenhaInput) {
      fireEvent.input(novaSenhaInput, { target: { value: 'novaSenha123' } });
    }
    if (confirmarInput) {
      fireEvent.input(confirmarInput, { target: { value: 'senhaDiferente' } });
    }

    const form = container.querySelector('form') as HTMLFormElement;
    fireEvent.submit(form);
    await new Promise(r => setTimeout(r, 0));

    expect(screen.getAllByText('As senhas não coincidem.').length).toBeGreaterThanOrEqual(1);
  });

  it('deve exibir erro quando API retorna 400', async () => {
    const { container } = await setup();

    httpTesting.expectOne('/api/v1/perfil').flush(perfilMock);
    await new Promise(r => setTimeout(r, 0));

    const form = container.querySelector('form') as HTMLFormElement;
    fireEvent.submit(form);

    httpTesting.expectOne({ method: 'PUT', url: '/api/v1/perfil' }).flush(
      { erro: 'Senha atual incorreta' },
      { status: 400, statusText: 'Bad Request' },
    );
    await new Promise(r => setTimeout(r, 0));

    expect(screen.getByText('Senha atual incorreta')).toBeTruthy();
  });

  it('deve exibir erro generico quando API retorna 500', async () => {
    const { container } = await setup();

    httpTesting.expectOne('/api/v1/perfil').flush(perfilMock);
    await new Promise(r => setTimeout(r, 0));

    const form = container.querySelector('form') as HTMLFormElement;
    fireEvent.submit(form);

    httpTesting.expectOne({ method: 'PUT', url: '/api/v1/perfil' }).flush(
      {},
      { status: 500, statusText: 'Internal Server Error' },
    );
    await new Promise(r => setTimeout(r, 0));

    expect(screen.getByText('Erro ao atualizar perfil. Tente novamente.')).toBeTruthy();
  });

  it('deve exibir titulo Meu Perfil', async () => {
    await setup();
    httpTesting.expectOne('/api/v1/perfil').flush(perfilMock);
    await new Promise(r => setTimeout(r, 0));

    expect(screen.getByText('Meu Perfil')).toBeTruthy();
  });

  it('deve exibir erro ao informar senha atual sem nova senha', async () => {
    const { container } = await setup();

    httpTesting.expectOne('/api/v1/perfil').flush(perfilMock);
    await new Promise(r => setTimeout(r, 0));

    // Preencher apenas senha atual (sem nova senha)
    const senhaAtualInput = container.querySelector('[data-cy="perfil-senha-atual"] input') as HTMLInputElement;
    if (senhaAtualInput) {
      fireEvent.input(senhaAtualInput, { target: { value: '12345678' } });
    }

    const form = container.querySelector('form') as HTMLFormElement;
    fireEvent.submit(form);
    await new Promise(r => setTimeout(r, 0));

    expect(screen.getByText('Informe a nova senha.')).toBeTruthy();
  });

  it('deve exibir erro quando API retorna 400 com detalhes array', async () => {
    const { container } = await setup();

    httpTesting.expectOne('/api/v1/perfil').flush(perfilMock);
    await new Promise(r => setTimeout(r, 0));

    const form = container.querySelector('form') as HTMLFormElement;
    fireEvent.submit(form);

    httpTesting.expectOne({ method: 'PUT', url: '/api/v1/perfil' }).flush(
      { detalhes: ['Campo nome é obrigatório', 'Senha muito curta'] },
      { status: 400, statusText: 'Bad Request' },
    );
    await new Promise(r => setTimeout(r, 0));

    expect(screen.getByText('Campo nome é obrigatório, Senha muito curta')).toBeTruthy();
  });

  it('deve exibir erro generico quando API retorna 400 sem erro e sem detalhes', async () => {
    const { container } = await setup();

    httpTesting.expectOne('/api/v1/perfil').flush(perfilMock);
    await new Promise(r => setTimeout(r, 0));

    const form = container.querySelector('form') as HTMLFormElement;
    fireEvent.submit(form);

    httpTesting.expectOne({ method: 'PUT', url: '/api/v1/perfil' }).flush(
      {},
      { status: 400, statusText: 'Bad Request' },
    );
    await new Promise(r => setTimeout(r, 0));

    expect(screen.getByText('Dados inválidos. Verifique os campos.')).toBeTruthy();
  });

  it('deve exibir erro de sessao expirada quando API retorna 401', async () => {
    const { container } = await setup();

    httpTesting.expectOne('/api/v1/perfil').flush(perfilMock);
    await new Promise(r => setTimeout(r, 0));

    const form = container.querySelector('form') as HTMLFormElement;
    fireEvent.submit(form);

    httpTesting.expectOne({ method: 'PUT', url: '/api/v1/perfil' }).flush(
      {},
      { status: 401, statusText: 'Unauthorized' },
    );
    await new Promise(r => setTimeout(r, 0));

    expect(screen.getByText('Sessão expirada. Faça login novamente.')).toBeTruthy();
  });

  it('deve exibir validacao de nome obrigatorio ao submeter com nome vazio', async () => {
    const { container } = await setup();

    httpTesting.expectOne('/api/v1/perfil').flush(perfilMock);
    await new Promise(r => setTimeout(r, 0));

    // Limpar o campo nome
    const nomeInput = container.querySelector('[data-cy="perfil-nome"]') as HTMLInputElement;
    fireEvent.input(nomeInput, { target: { value: '' } });

    const form = container.querySelector('form') as HTMLFormElement;
    fireEvent.submit(form);
    await new Promise(r => setTimeout(r, 0));

    expect(screen.getByText('Nome é obrigatório.')).toBeTruthy();
  });

  it('deve exibir validacao de nome minimo ao submeter com nome curto', async () => {
    const { container } = await setup();

    httpTesting.expectOne('/api/v1/perfil').flush(perfilMock);
    await new Promise(r => setTimeout(r, 0));

    // Preencher nome com 1 caractere
    const nomeInput = container.querySelector('[data-cy="perfil-nome"]') as HTMLInputElement;
    fireEvent.input(nomeInput, { target: { value: 'A' } });

    const form = container.querySelector('form') as HTMLFormElement;
    fireEvent.submit(form);
    await new Promise(r => setTimeout(r, 0));

    expect(screen.getByText('Nome deve ter pelo menos 2 caracteres.')).toBeTruthy();
  });

  it('deve exibir erro quando nova senha tem menos de 8 caracteres', async () => {
    const { container } = await setup();

    httpTesting.expectOne('/api/v1/perfil').flush(perfilMock);
    await new Promise(r => setTimeout(r, 0));

    // Preencher senha atual e nova senha curta
    const senhaAtualInput = container.querySelector('[data-cy="perfil-senha-atual"] input') as HTMLInputElement;
    const novaSenhaInput = container.querySelector('[data-cy="perfil-nova-senha"] input') as HTMLInputElement;
    const confirmarInput = container.querySelector('[data-cy="perfil-confirmar-nova-senha"] input') as HTMLInputElement;
    if (senhaAtualInput) {
      fireEvent.input(senhaAtualInput, { target: { value: '12345678' } });
    }
    if (novaSenhaInput) {
      fireEvent.input(novaSenhaInput, { target: { value: '1234' } });
    }
    if (confirmarInput) {
      fireEvent.input(confirmarInput, { target: { value: '1234' } });
    }

    const form = container.querySelector('form') as HTMLFormElement;
    fireEvent.submit(form);
    await new Promise(r => setTimeout(r, 0));

    expect(screen.getByText('A nova senha deve ter pelo menos 8 caracteres.')).toBeTruthy();
  });
});
