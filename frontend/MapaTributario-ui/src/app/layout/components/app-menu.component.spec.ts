import { render, screen } from '@testing-library/angular';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { PLATFORM_ID } from '@angular/core';
import { AppMenuComponent } from './app-menu.component';
import { RoleService } from '../../core/auth/role.service';
import { AuthService } from '../../core/auth/auth.service';

function criarTokenJwt(payload: Record<string, unknown>): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(JSON.stringify({ exp: Math.floor(Date.now() / 1000) + 3600, ...payload }));
  return `${header}.${body}.fake-signature`;
}

describe('AppMenuComponent', () => {
  const baseProviders = [
    provideHttpClient(),
    provideHttpClientTesting(),
    provideRouter([]),
    { provide: PLATFORM_ID, useValue: 'browser' },
  ];

  afterEach(() => {
    localStorage.clear();
    sessionStorage.clear();
  });

  it('deve renderizar o menu', async () => {
    const { container } = await render(AppMenuComponent, { providers: baseProviders });
    expect(container.querySelector('[data-cy="app-menu"]')).toBeTruthy();
  });

  it('deve conter classe layout-menu', async () => {
    const { container } = await render(AppMenuComponent, { providers: baseProviders });
    expect(container.querySelector('.layout-menu')).toBeTruthy();
  });

  it('deve exibir label da secao Menu', async () => {
    await render(AppMenuComponent, { providers: baseProviders });
    expect(screen.getByText('Menu')).toBeTruthy();
  });

  it('deve exibir item Consulta de Aliquotas', async () => {
    await render(AppMenuComponent, { providers: baseProviders });
    expect(screen.getByText('Consulta de Alíquotas')).toBeTruthy();
  });

  it('deve usar classes do Sakai para root menuitem', async () => {
    const { container } = await render(AppMenuComponent, { providers: baseProviders });
    expect(container.querySelector('.layout-root-menuitem')).toBeTruthy();
    expect(container.querySelector('.layout-menuitem-root-text')).toBeTruthy();
  });

  it('deve preservar classe layout-menuitem-icon junto com icone dinamico', async () => {
    const { container } = await render(AppMenuComponent, { providers: baseProviders });
    const icon = container.querySelector('.layout-menuitem-icon') as HTMLElement;
    expect(icon).toBeTruthy();
    expect(icon.classList.contains('pi')).toBe(true);
    expect(icon.classList.contains('pi-map')).toBe(true);
  });

  it('deve usar classe layout-menuitem-text no label', async () => {
    const { container } = await render(AppMenuComponent, { providers: baseProviders });
    expect(container.querySelector('.layout-menuitem-text')).toBeTruthy();
  });

  it('nao deve exibir secao Administracao para usuario comum', async () => {
    await render(AppMenuComponent, { providers: baseProviders });
    expect(screen.queryByText('Administração')).toBeNull();
  });

  it('deve exibir secao Administracao para usuario Admin', async () => {
    const token = criarTokenJwt({ role: 'Admin' });
    localStorage.setItem('rememberMe', 'true');
    localStorage.setItem('accessToken', token);

    await render(AppMenuComponent, {
      providers: [
        ...baseProviders,
        {
          provide: AuthService,
          useValue: {
            isAuthenticated: () => true,
            getAccessToken: () => token,
          },
        },
      ],
    });
    expect(screen.getByText('Administração')).toBeTruthy();
    expect(screen.getByText('Crawler')).toBeTruthy();
    expect(screen.getByText('Certificado')).toBeTruthy();
  });

  it('nao deve exibir Crawler para usuario com role User', async () => {
    const token = criarTokenJwt({ role: 'User' });
    localStorage.setItem('rememberMe', 'true');
    localStorage.setItem('accessToken', token);

    await render(AppMenuComponent, {
      providers: [
        ...baseProviders,
        {
          provide: AuthService,
          useValue: {
            isAuthenticated: () => true,
            getAccessToken: () => token,
          },
        },
      ],
    });
    expect(screen.queryByText('Administração')).toBeNull();
    expect(screen.queryByText('Crawler')).toBeNull();
  });
});
