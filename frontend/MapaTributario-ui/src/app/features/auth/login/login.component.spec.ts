import { render, screen, fireEvent } from '@testing-library/angular';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { PLATFORM_ID } from '@angular/core';
import { LoginComponent } from './login.component';

describe('LoginComponent', () => {
  async function setup() {
    const renderResult = await render(LoginComponent, {
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([
          { path: '', component: {} as any },
          { path: 'auth/login', component: LoginComponent },
          { path: 'auth/signup', component: {} as any },
        ]),
        { provide: PLATFORM_ID, useValue: 'browser' },
      ],
    });
    const httpTesting = TestBed.inject(HttpTestingController);
    const router = TestBed.inject(Router);
    return { ...renderResult, httpTesting, router };
  }

  afterEach(() => localStorage.clear());

  it('deve renderizar pagina de login', async () => {
    const { container } = await setup();
    expect(container.querySelector('[data-cy="login-page"]')).toBeTruthy();
  });

  it('deve exibir titulo Mapa Tributario', async () => {
    await setup();
    expect(screen.getByText('Mapa Tributário')).toBeTruthy();
  });

  it('deve exibir subtitulo', async () => {
    await setup();
    expect(screen.getByText('Entre para continuar')).toBeTruthy();
  });

  it('deve ter campo de email', async () => {
    await setup();
    expect(screen.getByLabelText('Email')).toBeTruthy();
  });

  it('deve ter campo de senha', async () => {
    const { container } = await setup();
    expect(container.querySelector('p-password#senha')).toBeTruthy();
  });

  it('deve ter checkbox lembrar-me', async () => {
    const { container } = await setup();
    expect(container.querySelector('p-checkbox#lembrar')).toBeTruthy();
  });

  it('deve ter botao Entrar', async () => {
    await setup();
    expect(screen.getByRole('button', { name: 'Entrar' })).toBeTruthy();
  });

  it('deve ter link para criar conta', async () => {
    await setup();
    expect(screen.getByText('Criar conta')).toBeTruthy();
  });

  it('deve marcar campos como touched ao submeter form invalido', async () => {
    const { fixture } = await setup();
    const component = fixture.componentInstance;

    component.onSubmit();

    expect(component.form.controls.email.touched).toBe(true);
    expect(component.form.controls.senha.touched).toBe(true);
    expect(component.loading()).toBe(false);
  });

  it('deve nao submeter com form invalido', async () => {
    const { fixture, httpTesting } = await setup();
    const component = fixture.componentInstance;

    component.onSubmit();

    httpTesting.expectNone('/api/v1/auth/login');
  });

  it('deve submeter login com sucesso e navegar', async () => {
    const { fixture, httpTesting, router } = await setup();
    const component = fixture.componentInstance;
    const navigateSpy = vi.spyOn(router, 'navigate');

    const header = btoa(JSON.stringify({ alg: 'HS256' }));
    const payload = btoa(JSON.stringify({ sub: '1', email: 'a@b.com', name: 'User', exp: Math.floor(Date.now() / 1000) + 3600 }));
    const token = `${header}.${payload}.sig`;

    component.form.setValue({ email: 'a@b.com', senha: 'password123', lembrar: false });
    component.onSubmit();

    expect(component.loading()).toBe(true);

    httpTesting.expectOne('/api/v1/auth/login').flush({ accessToken: token, refreshToken: 'r', expiresIn: 3600 });

    expect(component.loading()).toBe(false);
    expect(navigateSpy).toHaveBeenCalledWith(['/']);
  });

  it('deve exibir erro 401', async () => {
    const { fixture, httpTesting } = await setup();
    const component = fixture.componentInstance;

    component.form.setValue({ email: 'a@b.com', senha: 'wrongpass1', lembrar: false });
    component.onSubmit();

    httpTesting.expectOne('/api/v1/auth/login').flush(
      { erro: 'Inválido' },
      { status: 401, statusText: 'Unauthorized' },
    );

    expect(component.errorMessage()).toBe('Email ou senha inválidos.');
    expect(component.loading()).toBe(false);
  });

  it('deve exibir erro 403', async () => {
    const { fixture, httpTesting } = await setup();
    const component = fixture.componentInstance;

    component.form.setValue({ email: 'a@b.com', senha: 'password123', lembrar: false });
    component.onSubmit();

    httpTesting.expectOne('/api/v1/auth/login').flush(
      { erro: 'Inativo' },
      { status: 403, statusText: 'Forbidden' },
    );

    expect(component.errorMessage()).toBe('Conta inativa. Entre em contato com o suporte.');
  });

  it('deve exibir erro generico para outros status', async () => {
    const { fixture, httpTesting } = await setup();
    const component = fixture.componentInstance;

    component.form.setValue({ email: 'a@b.com', senha: 'password123', lembrar: false });
    component.onSubmit();

    httpTesting.expectOne('/api/v1/auth/login').flush(null, { status: 500, statusText: 'Server Error' });

    expect(component.errorMessage()).toBe('Erro ao realizar login. Tente novamente.');
  });

  it('deve validar email obrigatorio', async () => {
    const { fixture } = await setup();
    const component = fixture.componentInstance;

    component.form.controls.email.markAsTouched();
    expect(component.form.controls.email.errors?.['required']).toBeTruthy();
  });

  it('deve validar formato de email', async () => {
    const { fixture } = await setup();
    const component = fixture.componentInstance;

    component.form.controls.email.setValue('not-email');
    component.form.controls.email.markAsTouched();
    expect(component.form.controls.email.errors?.['email']).toBeTruthy();
  });

  it('deve validar tamanho minimo da senha', async () => {
    const { fixture } = await setup();
    const component = fixture.componentInstance;

    component.form.controls.senha.setValue('abc');
    component.form.controls.senha.markAsTouched();
    expect(component.form.controls.senha.errors?.['minlength']).toBeTruthy();
  });
});
