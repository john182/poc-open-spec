import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { PLATFORM_ID } from '@angular/core';
import { RoleService } from './role.service';
import { AuthService } from './auth.service';

function criarTokenJwt(payload: Record<string, unknown>): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(JSON.stringify({ exp: Math.floor(Date.now() / 1000) + 3600, ...payload }));
  return `${header}.${body}.fake-signature`;
}

describe('RoleService', () => {
  let roleService: RoleService;
  let authService: AuthService;

  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: PLATFORM_ID, useValue: 'browser' },
      ],
    });
    roleService = TestBed.inject(RoleService);
    authService = TestBed.inject(AuthService);
  });

  afterEach(() => {
    localStorage.clear();
    sessionStorage.clear();
  });

  it('deve retornar null quando nao autenticado', () => {
    Object.defineProperty(authService, 'isAuthenticated', { value: () => false });
    expect(roleService.role()).toBeNull();
    expect(roleService.isAdmin()).toBe(false);
  });

  it('deve retornar Admin quando token tem role Admin (claim Microsoft)', () => {
    const token = criarTokenJwt({
      'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'Admin',
    });
    localStorage.setItem('rememberMe', 'true');
    localStorage.setItem('accessToken', token);
    Object.defineProperty(authService, 'isAuthenticated', { value: () => true });

    expect(roleService.role()).toBe('Admin');
    expect(roleService.isAdmin()).toBe(true);
  });

  it('deve retornar Admin quando token tem role Admin (claim simples)', () => {
    const token = criarTokenJwt({ role: 'Admin' });
    localStorage.setItem('rememberMe', 'true');
    localStorage.setItem('accessToken', token);
    Object.defineProperty(authService, 'isAuthenticated', { value: () => true });

    expect(roleService.role()).toBe('Admin');
    expect(roleService.isAdmin()).toBe(true);
  });

  it('deve retornar User quando token nao tem role Admin', () => {
    const token = criarTokenJwt({ role: 'User' });
    localStorage.setItem('rememberMe', 'true');
    localStorage.setItem('accessToken', token);
    Object.defineProperty(authService, 'isAuthenticated', { value: () => true });

    expect(roleService.role()).toBe('User');
    expect(roleService.isAdmin()).toBe(false);
  });

  it('deve retornar User quando token nao tem claim role', () => {
    const token = criarTokenJwt({ sub: '123', email: 'user@test.com' });
    localStorage.setItem('rememberMe', 'true');
    localStorage.setItem('accessToken', token);
    Object.defineProperty(authService, 'isAuthenticated', { value: () => true });

    expect(roleService.role()).toBe('User');
    expect(roleService.isAdmin()).toBe(false);
  });
});
