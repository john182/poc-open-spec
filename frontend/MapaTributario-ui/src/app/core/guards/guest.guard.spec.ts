import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PLATFORM_ID } from '@angular/core';
import { guestGuard } from './guest.guard';
import { AuthService } from '../auth/auth.service';
import { ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree } from '@angular/router';

describe('guestGuard', () => {
  const dummyRoute = {} as ActivatedRouteSnapshot;
  const dummyState = {} as RouterStateSnapshot;

  function setup(isAuthenticated: boolean, platformId = 'browser') {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: '', component: {} as any },
          { path: 'auth/login', component: {} as any },
        ]),
        { provide: PLATFORM_ID, useValue: platformId },
        {
          provide: AuthService,
          useValue: { isAuthenticated: () => isAuthenticated },
        },
      ],
    });
    return TestBed.runInInjectionContext(() => guestGuard(dummyRoute, dummyState));
  }

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('deve permitir acesso quando nao autenticado', () => {
    const result = setup(false);
    expect(result).toBe(true);
  });

  it('deve redirecionar para / quando autenticado', () => {
    const result = setup(true);
    expect(result).toBeInstanceOf(UrlTree);
    expect((result as UrlTree).toString()).toBe('/');
  });

  it('deve permitir acesso no servidor (SSR)', () => {
    const result = setup(true, 'server');
    expect(result).toBe(true);
  });
});
