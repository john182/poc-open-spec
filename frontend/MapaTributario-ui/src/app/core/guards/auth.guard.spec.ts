import { TestBed } from '@angular/core/testing';
import { UrlTree } from '@angular/router';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { PLATFORM_ID } from '@angular/core';
import { authGuard } from './auth.guard';
import { AuthService } from '../auth/auth.service';

describe('authGuard', () => {
  let authService: AuthService;
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([
          { path: 'auth/login', component: {} as any },
          { path: 'protected', component: {} as any, canActivate: [authGuard] },
        ]),
        { provide: PLATFORM_ID, useValue: 'browser' },
      ],
    });
    authService = TestBed.inject(AuthService);
  });

  afterEach(() => localStorage.clear());

  it('deve retornar true quando autenticado', () => {
    // Simulate authenticated state
    Object.defineProperty(authService, 'isAuthenticated', { value: () => true });

    const result = TestBed.runInInjectionContext(() => authGuard({} as any, {} as any));
    expect(result).toBe(true);
  });

  it('deve redirecionar para /auth/login quando nao autenticado', () => {
    Object.defineProperty(authService, 'isAuthenticated', { value: () => false });

    const result = TestBed.runInInjectionContext(() => authGuard({} as any, {} as any));
    expect(result).toBeInstanceOf(UrlTree);
    expect((result as UrlTree).toString()).toBe('/auth/login');
  });
});
