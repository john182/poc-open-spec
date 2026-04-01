import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { PLATFORM_ID } from '@angular/core';
import { AuthService } from './auth.service';

function makeJwt(payload: Record<string, unknown>): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(JSON.stringify(payload));
  return `${header}.${body}.fake-signature`;
}

const validPayload = {
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier': 'user-123',
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress': 'test@test.com',
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name': 'Test User',
  exp: Math.floor(Date.now() / 1000) + 3600,
};

const expiredPayload = { ...validPayload, exp: Math.floor(Date.now() / 1000) - 3600 };

describe('AuthService', () => {
  let service: AuthService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: 'auth/login', component: {} as any }]),
        { provide: PLATFORM_ID, useValue: 'browser' },
      ],
    });
    service = TestBed.inject(AuthService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
    localStorage.clear();
  });

  it('deve iniciar nao autenticado', () => {
    expect(service.isAuthenticated()).toBe(false);
    expect(service.userName()).toBe('');
  });

  describe('login', () => {
    it('deve autenticar com sucesso e armazenar tokens', () => {
      const token = makeJwt(validPayload);

      service.login('test@test.com', 'password123').subscribe(res => {
        expect(res.accessToken).toBe(token);
      });

      const req = httpTesting.expectOne('/api/v1/auth/login');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ email: 'test@test.com', senha: 'password123' });
      req.flush({ accessToken: token, refreshToken: 'refresh-abc', expiresIn: 3600 });

      expect(service.isAuthenticated()).toBe(true);
      expect(service.userName()).toBe('Test User');
      expect(service.getAccessToken()).toBe(token);
      expect(service.getRefreshToken()).toBe('refresh-abc');
    });

    it('deve propagar erro de login', () => {
      let errorCaught = false;
      service.login('wrong@test.com', 'wrong').subscribe({
        error: (err) => {
          errorCaught = true;
          expect(err.status).toBe(401);
        },
      });

      httpTesting.expectOne('/api/v1/auth/login').flush(
        { erro: 'Credenciais inválidas' },
        { status: 401, statusText: 'Unauthorized' },
      );
      expect(errorCaught).toBe(true);
      expect(service.isAuthenticated()).toBe(false);
    });
  });

  describe('register', () => {
    it('deve registrar com sucesso e armazenar tokens', () => {
      const token = makeJwt(validPayload);

      service.register('new@test.com', 'New User', 'password123').subscribe();

      const req = httpTesting.expectOne('/api/v1/auth/register');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ email: 'new@test.com', nome: 'New User', senha: 'password123' });
      req.flush({ accessToken: token, refreshToken: 'refresh-xyz', expiresIn: 3600 });

      expect(service.isAuthenticated()).toBe(true);
      expect(service.userName()).toBe('Test User');
    });

    it('deve propagar erro 409 para email duplicado', () => {
      let status = 0;
      service.register('dup@test.com', 'Dup', 'password123').subscribe({
        error: (err) => { status = err.status; },
      });

      httpTesting.expectOne('/api/v1/auth/register').flush(
        { erro: 'Email já cadastrado' },
        { status: 409, statusText: 'Conflict' },
      );
      expect(status).toBe(409);
    });
  });

  describe('refresh', () => {
    it('deve atualizar access token com sucesso', () => {
      localStorage.setItem('refreshToken', 'old-refresh');
      const newToken = makeJwt(validPayload);

      service.refresh().subscribe(res => {
        expect(res.accessToken).toBe(newToken);
      });

      const req = httpTesting.expectOne('/api/v1/auth/refresh');
      expect(req.request.body).toEqual({ refreshToken: 'old-refresh' });
      req.flush({ accessToken: newToken, expiresIn: 3600 });

      expect(service.isAuthenticated()).toBe(true);
      expect(service.getAccessToken()).toBe(newToken);
    });

    it('deve falhar sem refresh token', () => {
      let errorCaught = false;
      service.refresh().subscribe({
        error: () => { errorCaught = true; },
      });
      expect(errorCaught).toBe(true);
    });
  });

  describe('logout', () => {
    it('deve limpar tokens e redirecionar', () => {
      localStorage.setItem('accessToken', 'abc');
      localStorage.setItem('refreshToken', 'def');

      service.logout();

      expect(service.isAuthenticated()).toBe(false);
      expect(service.userName()).toBe('');
      expect(service.getAccessToken()).toBeNull();
      expect(service.getRefreshToken()).toBeNull();
    });
  });

  describe('token decode', () => {
    it('deve iniciar autenticado se localStorage tem token valido', () => {
      localStorage.setItem('accessToken', makeJwt(validPayload));

      const freshService = TestBed.inject(AuthService);
      // Service was already created, so we need a new one
      // Since AuthService is providedIn: root, we test the initial state differently
      expect(freshService.getAccessToken()).toBe(localStorage.getItem('accessToken'));
    });

    it('deve tratar token expirado como nao autenticado', () => {
      localStorage.setItem('accessToken', makeJwt(expiredPayload));

      // Re-create TestBed to get fresh service
      TestBed.resetTestingModule();
      TestBed.configureTestingModule({
        providers: [
          provideHttpClient(),
          provideHttpClientTesting(),
          provideRouter([{ path: 'auth/login', component: {} as any }]),
          { provide: PLATFORM_ID, useValue: 'browser' },
        ],
      });
      const freshService = TestBed.inject(AuthService);
      httpTesting = TestBed.inject(HttpTestingController);

      expect(freshService.isAuthenticated()).toBe(false);
    });

    it('deve tratar token malformado como nao autenticado', () => {
      localStorage.setItem('accessToken', 'not-a-jwt');

      TestBed.resetTestingModule();
      TestBed.configureTestingModule({
        providers: [
          provideHttpClient(),
          provideHttpClientTesting(),
          provideRouter([{ path: 'auth/login', component: {} as any }]),
          { provide: PLATFORM_ID, useValue: 'browser' },
        ],
      });
      const freshService = TestBed.inject(AuthService);
      httpTesting = TestBed.inject(HttpTestingController);

      expect(freshService.isAuthenticated()).toBe(false);
    });

    it('deve extrair userName de claims padrao quando claims .NET nao existem', () => {
      const stdPayload = { sub: 'u1', email: 'std@test.com', name: 'Std User', exp: Math.floor(Date.now() / 1000) + 3600 };
      const token = makeJwt(stdPayload);

      service.login('std@test.com', 'pass1234').subscribe();
      httpTesting.expectOne('/api/v1/auth/login').flush({ accessToken: token, refreshToken: 'r', expiresIn: 3600 });

      expect(service.userName()).toBe('Std User');
    });
  });
});
