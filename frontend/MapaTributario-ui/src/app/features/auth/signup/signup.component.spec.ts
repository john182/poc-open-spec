import { render, screen, waitFor } from '@testing-library/angular';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { PLATFORM_ID } from '@angular/core';
import { SignupComponent } from './signup.component';

describe('SignupComponent', () => {
  async function setup() {
    const renderResult = await render(SignupComponent, {
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([
          { path: '', component: {} as any },
          { path: 'auth/login', component: {} as any },
          { path: 'auth/signup', component: SignupComponent },
        ]),
        { provide: PLATFORM_ID, useValue: 'browser' },
      ],
    });
    const httpTesting = TestBed.inject(HttpTestingController);
    const router = TestBed.inject(Router);
    return { ...renderResult, httpTesting, router };
  }

  afterEach(() => localStorage.clear());

  it('deve renderizar pagina de signup', async () => {
    const { container } = await setup();
    expect(container.querySelector('[data-cy="signup-page"]')).toBeTruthy();
  });

  it('deve exibir titulo Criar Conta', async () => {
    const { container } = await setup();
    expect(container.querySelector('.text-3xl')?.textContent).toBe('Criar Conta');
  });

  it('deve ter campo de nome', async () => {
    await setup();
    expect(screen.getByLabelText('Nome')).toBeTruthy();
  });

  it('deve ter campo de email', async () => {
    await setup();
    expect(screen.getByLabelText('Email')).toBeTruthy();
  });

  it('deve ter campo de senha', async () => {
    const { container } = await setup();
    expect(container.querySelector('p-password#senha')).toBeTruthy();
  });

  it('deve ter campo de confirmar senha', async () => {
    const { container } = await setup();
    expect(container.querySelector('p-password#confirmar')).toBeTruthy();
  });

  it('deve ter botao Criar Conta', async () => {
    await setup();
    expect(screen.getByRole('button', { name: 'Criar Conta' })).toBeTruthy();
  });

  it('deve ter link para entrar', async () => {
    await setup();
    expect(screen.getByText('Entrar')).toBeTruthy();
  });

  it('deve marcar campos como touched ao submeter form invalido', async () => {
    const { fixture } = await setup();
    const component = fixture.componentInstance;

    component.onSubmit();

    expect(component.form.controls.nome.touched).toBe(true);
    expect(component.form.controls.email.touched).toBe(true);
    expect(component.loading()).toBe(false);
  });

  it('deve nao submeter com form invalido', async () => {
    const { fixture, httpTesting } = await setup();
    const component = fixture.componentInstance;

    component.onSubmit();

    httpTesting.expectNone('/api/v1/auth/register');
  });

  it('deve registrar com sucesso e navegar', async () => {
    const { fixture, httpTesting, router } = await setup();
    const component = fixture.componentInstance;
    const navigateSpy = vi.spyOn(router, 'navigate');

    const header = btoa(JSON.stringify({ alg: 'HS256' }));
    const payload = btoa(JSON.stringify({ sub: '1', email: 'new@b.com', name: 'New User', exp: Math.floor(Date.now() / 1000) + 3600 }));
    const token = `${header}.${payload}.sig`;

    component.form.setValue({ nome: 'New User', email: 'new@b.com', senha: 'password123', confirmarSenha: 'password123' });
    component.onSubmit();

    expect(component.loading()).toBe(true);

    const req = httpTesting.expectOne('/api/v1/auth/register');
    expect(req.request.body).toEqual({ email: 'new@b.com', nome: 'New User', senha: 'password123' });
    req.flush({ accessToken: token, refreshToken: 'r', expiresIn: 3600 });

    expect(component.loading()).toBe(false);
    expect(navigateSpy).toHaveBeenCalledWith(['/']);
  });

  it('deve exibir erro 409 para email duplicado', async () => {
    const { fixture, httpTesting } = await setup();
    const component = fixture.componentInstance;

    component.form.setValue({ nome: 'User', email: 'dup@b.com', senha: 'password123', confirmarSenha: 'password123' });
    component.onSubmit();

    httpTesting.expectOne('/api/v1/auth/register').flush(
      { erro: 'Email já cadastrado' },
      { status: 409, statusText: 'Conflict' },
    );

    expect(component.errorMessage()).toBe('Este email já está cadastrado.');
  });

  it('deve exibir detalhes de validacao para erro 400', async () => {
    const { fixture, httpTesting } = await setup();
    const component = fixture.componentInstance;

    component.form.setValue({ nome: 'User', email: 'a@b.com', senha: 'password123', confirmarSenha: 'password123' });
    component.onSubmit();

    httpTesting.expectOne('/api/v1/auth/register').flush(
      { detalhes: ['Nome muito curto', 'Email inválido'] },
      { status: 400, statusText: 'Bad Request' },
    );

    expect(component.errorMessage()).toBe('Nome muito curto, Email inválido');
  });

  it('deve exibir mensagem padrao para erro 400 sem detalhes', async () => {
    const { fixture, httpTesting } = await setup();
    const component = fixture.componentInstance;

    component.form.setValue({ nome: 'User', email: 'a@b.com', senha: 'password123', confirmarSenha: 'password123' });
    component.onSubmit();

    httpTesting.expectOne('/api/v1/auth/register').flush(
      { erro: 'Inválido' },
      { status: 400, statusText: 'Bad Request' },
    );

    expect(component.errorMessage()).toBe('Dados inválidos. Verifique os campos.');
  });

  it('deve exibir erro generico para outros status', async () => {
    const { fixture, httpTesting } = await setup();
    const component = fixture.componentInstance;

    component.form.setValue({ nome: 'User', email: 'a@b.com', senha: 'password123', confirmarSenha: 'password123' });
    component.onSubmit();

    httpTesting.expectOne('/api/v1/auth/register').flush(null, { status: 500, statusText: 'Server Error' });

    expect(component.errorMessage()).toBe('Erro ao criar conta. Tente novamente.');
  });

  it('deve validar nome obrigatorio', async () => {
    const { fixture } = await setup();
    const component = fixture.componentInstance;

    component.form.controls.nome.markAsTouched();
    expect(component.form.controls.nome.errors?.['required']).toBeTruthy();
  });

  it('deve validar tamanho minimo do nome', async () => {
    const { fixture } = await setup();
    const component = fixture.componentInstance;

    component.form.controls.nome.setValue('A');
    expect(component.form.controls.nome.errors?.['minlength']).toBeTruthy();
  });

  it('deve validar email obrigatorio', async () => {
    const { fixture } = await setup();
    const component = fixture.componentInstance;

    component.form.controls.email.markAsTouched();
    expect(component.form.controls.email.errors?.['required']).toBeTruthy();
  });

  it('deve validar tamanho minimo da senha', async () => {
    const { fixture } = await setup();
    const component = fixture.componentInstance;

    component.form.controls.senha.setValue('short');
    expect(component.form.controls.senha.errors?.['minlength']).toBeTruthy();
  });

  it('deve validar senhas diferentes (passwordMismatch)', async () => {
    const { fixture } = await setup();
    const component = fixture.componentInstance;

    component.form.controls.senha.setValue('password123');
    component.form.controls.confirmarSenha.setValue('different1');
    expect(component.form.errors?.['passwordMismatch']).toBeTruthy();
  });

  it('deve validar senhas iguais como valido', async () => {
    const { fixture } = await setup();
    const component = fixture.componentInstance;

    component.form.controls.senha.setValue('password123');
    component.form.controls.confirmarSenha.setValue('password123');
    expect(component.form.errors?.['passwordMismatch']).toBeFalsy();
  });

  it('Given_NomeTouched_Should_ExibirMensagemNomeObrigatorio', async () => {
    // Arrange
    const { fixture } = await setup();
    const componente = fixture.componentInstance;

    // Act
    componente.form.controls.nome.markAsTouched();
    fixture.detectChanges();

    // Assert
    await waitFor(() => {
      expect(screen.getByText(/Nome é obrigatório/)).toBeTruthy();
    });
  });

  it('Given_NomeCurto_Should_ExibirMensagemTamanhoMinimo', async () => {
    // Arrange
    const { fixture } = await setup();
    const componente = fixture.componentInstance;

    // Act
    componente.form.controls.nome.setValue('A');
    componente.form.controls.nome.markAsTouched();
    fixture.detectChanges();

    // Assert
    await waitFor(() => {
      expect(screen.getByText(/Nome deve ter pelo menos 2 caracteres/)).toBeTruthy();
    });
  });

  it('Given_EmailTouched_Should_ExibirMensagemEmailObrigatorio', async () => {
    // Arrange
    const { fixture } = await setup();
    const componente = fixture.componentInstance;

    // Act
    componente.form.controls.email.markAsTouched();
    fixture.detectChanges();

    // Assert
    await waitFor(() => {
      expect(screen.getByText(/Email é obrigatório/)).toBeTruthy();
    });
  });

  it('Given_EmailInvalido_Should_ExibirMensagemEmailInvalido', async () => {
    // Arrange
    const { fixture } = await setup();
    const componente = fixture.componentInstance;

    // Act
    componente.form.controls.email.setValue('invalid-email');
    componente.form.controls.email.markAsTouched();
    fixture.detectChanges();

    // Assert
    await waitFor(() => {
      expect(screen.getByText(/Email inválido/)).toBeTruthy();
    });
  });

  it('Given_SenhaTouched_Should_ExibirMensagemSenhaObrigatoria', async () => {
    // Arrange
    const { fixture } = await setup();
    const componente = fixture.componentInstance;

    // Act
    componente.form.controls.senha.markAsTouched();
    fixture.detectChanges();

    // Assert
    await waitFor(() => {
      expect(screen.getByText(/Senha é obrigatória/)).toBeTruthy();
    });
  });

  it('Given_SenhaCurta_Should_ExibirMensagemTamanhoMinimo', async () => {
    // Arrange
    const { fixture } = await setup();
    const componente = fixture.componentInstance;

    // Act
    componente.form.controls.senha.setValue('short');
    componente.form.controls.senha.markAsTouched();
    fixture.detectChanges();

    // Assert
    await waitFor(() => {
      expect(screen.getByText(/Senha deve ter pelo menos 8 caracteres/)).toBeTruthy();
    });
  });

  it('Given_ConfirmarSenhaTouched_Should_ExibirMensagemConfirmacaoObrigatoria', async () => {
    // Arrange
    const { fixture } = await setup();
    const componente = fixture.componentInstance;

    // Act
    componente.form.controls.confirmarSenha.markAsTouched();
    fixture.detectChanges();

    // Assert
    await waitFor(() => {
      expect(screen.getByText(/Confirmação é obrigatória/)).toBeTruthy();
    });
  });

  it('Given_SenhasDiferentes_Should_ExibirMensagemSenhasNaoCoincidem', async () => {
    // Arrange
    const { fixture } = await setup();
    const componente = fixture.componentInstance;

    // Act
    componente.form.controls.senha.setValue('password123');
    componente.form.controls.confirmarSenha.setValue('different1');
    componente.form.controls.confirmarSenha.markAsTouched();
    fixture.detectChanges();

    // Assert
    await waitFor(() => {
      expect(screen.getByText(/senhas não coincidem/)).toBeTruthy();
    });
  });

  it('Given_ErroNoSubmit_Should_ExibirMensagemErroNoTemplate', async () => {
    // Arrange
    const { fixture, httpTesting } = await setup();
    const componente = fixture.componentInstance;

    componente.form.setValue({ nome: 'User', email: 'a@b.com', senha: 'password123', confirmarSenha: 'password123' });
    componente.onSubmit();

    httpTesting.expectOne('/api/v1/auth/register').flush(null, { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();

    // Assert
    await waitFor(() => {
      expect(screen.getByText(/Erro ao criar conta/)).toBeTruthy();
    });
  });

  it('Given_LoadingTrue_Should_DesabilitarBotaoSubmit', async () => {
    // Arrange
    const { fixture, httpTesting } = await setup();
    const componente = fixture.componentInstance;

    componente.form.setValue({ nome: 'User', email: 'a@b.com', senha: 'password123', confirmarSenha: 'password123' });

    // Act
    componente.onSubmit();
    fixture.detectChanges();

    // Assert
    expect(componente.loading()).toBe(true);

    // Finalizar requisição
    httpTesting.expectOne('/api/v1/auth/register').flush(null, { status: 500, statusText: 'Server Error' });

    expect(componente.loading()).toBe(false);
  });
});
