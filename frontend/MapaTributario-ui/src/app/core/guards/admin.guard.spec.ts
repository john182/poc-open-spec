import { TestBed } from '@angular/core/testing';
import { UrlTree } from '@angular/router';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { PLATFORM_ID } from '@angular/core';
import { adminGuard } from './admin.guard';
import { AuthService } from '../auth/auth.service';
import { RoleService } from '../auth/role.service';

describe('adminGuard', () => {
  let authService: AuthService;
  let roleService: RoleService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([
          { path: 'auth/login', component: {} as any },
          { path: 'acesso-negado', component: {} as any },
          { path: 'admin', component: {} as any, canActivate: [adminGuard] },
        ]),
        { provide: PLATFORM_ID, useValue: 'browser' },
      ],
    });
    authService = TestBed.inject(AuthService);
    roleService = TestBed.inject(RoleService);
  });

  afterEach(() => localStorage.clear());

  it('deve retornar true quando usuario eh Admin', () => {
    Object.defineProperty(authService, 'isAuthenticated', { value: () => true });
    Object.defineProperty(roleService, 'isAdmin', { value: () => true });

    const result = TestBed.runInInjectionContext(() => adminGuard({} as any, {} as any));
    expect(result).toBe(true);
  });

  it('deve redirecionar para /acesso-negado quando usuario nao eh Admin', () => {
    Object.defineProperty(authService, 'isAuthenticated', { value: () => true });
    Object.defineProperty(roleService, 'isAdmin', { value: () => false });

    const result = TestBed.runInInjectionContext(() => adminGuard({} as any, {} as any));
    expect(result).toBeInstanceOf(UrlTree);
    expect((result as UrlTree).toString()).toBe('/acesso-negado');
  });

  it('deve redirecionar para /auth/login quando nao autenticado', () => {
    Object.defineProperty(authService, 'isAuthenticated', { value: () => false });

    const result = TestBed.runInInjectionContext(() => adminGuard({} as any, {} as any));
    expect(result).toBeInstanceOf(UrlTree);
    expect((result as UrlTree).toString()).toBe('/auth/login');
  });
});
