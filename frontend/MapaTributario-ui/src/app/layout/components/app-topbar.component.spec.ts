import { render, screen, fireEvent } from '@testing-library/angular';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { PLATFORM_ID } from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { AppTopbarComponent } from './app-topbar.component';
import { LayoutService } from '../services/layout.service';
import { AuthService } from '../../core/auth/auth.service';

describe('AppTopbarComponent', () => {
  async function setup() {
    const result = await render(AppTopbarComponent, {
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAnimationsAsync(),
        provideRouter([
          { path: '', component: {} as any },
          { path: 'auth/login', component: {} as any },
        ]),
        { provide: PLATFORM_ID, useValue: 'browser' },
      ],
    });
    const layoutService = TestBed.inject(LayoutService);
    const authService = TestBed.inject(AuthService);
    const router = TestBed.inject(Router);
    return { ...result, layoutService, authService, router };
  }

  afterEach(() => localStorage.clear());

  it('deve renderizar o topbar', async () => {
    const { container } = await setup();
    expect(container.querySelector('[data-cy="app-topbar"]')).toBeTruthy();
  });

  it('deve exibir nome da aplicacao', async () => {
    await setup();
    expect(screen.getByText('Mapa Tributário')).toBeTruthy();
  });

  it('deve ter botao de menu toggle com acessibilidade', async () => {
    const { container } = await setup();
    const btn = container.querySelector('.layout-menu-button') as HTMLButtonElement;
    expect(btn).toBeTruthy();
    expect(btn.getAttribute('type')).toBe('button');
    expect(btn.getAttribute('aria-label')).toBe('Alternar menu');
  });

  it('deve ter botao de dark mode com acessibilidade', async () => {
    const { container } = await setup();
    const btn = container.querySelector('.layout-config-menu button') as HTMLButtonElement;
    expect(btn).toBeTruthy();
    expect(btn.getAttribute('type')).toBe('button');
    expect(btn.getAttribute('aria-label')).toBeTruthy();
    expect(btn.hasAttribute('aria-pressed')).toBe(true);
  });

  it('Given_CliqueBotaoMenu_Should_ChamarOnMenuToggle', async () => {
    // Arrange
    const { container, layoutService } = await setup();
    const espiao = vi.spyOn(layoutService, 'onMenuToggle');
    const botaoMenu = container.querySelector('.layout-menu-button') as HTMLButtonElement;

    // Act
    fireEvent.click(botaoMenu);

    // Assert
    expect(espiao).toHaveBeenCalledTimes(1);
  });

  it('Given_CliqueBotaoDarkMode_Should_AlternarTema', async () => {
    // Arrange
    const { container, layoutService } = await setup();
    const temaInicial = layoutService.isDarkTheme();

    // Encontrar o botão de dark mode (segundo botão na config-menu)
    const botoesDarkMode = container.querySelectorAll('.layout-config-menu button');
    // O primeiro botão com aria-label contendo "tema" é o dark mode
    let botaoDarkMode: HTMLButtonElement | null = null;
    botoesDarkMode.forEach(btn => {
      const ariaLabel = btn.getAttribute('aria-label') ?? '';
      if (ariaLabel.includes('tema')) {
        botaoDarkMode = btn as HTMLButtonElement;
      }
    });
    expect(botaoDarkMode).toBeTruthy();

    // Act
    fireEvent.click(botaoDarkMode!);

    // Assert
    expect(layoutService.isDarkTheme()).toBe(!temaInicial);
  });

  it('Given_CliqueBotaoLogout_Should_ChamarLogout', async () => {
    // Arrange — simular token válido para que o menu do usuário apareça
    const header = btoa(JSON.stringify({ alg: 'HS256' }));
    const payload = btoa(JSON.stringify({ sub: '1', email: 'user@test.com', name: 'João Silva', exp: Math.floor(Date.now() / 1000) + 3600 }));
    const token = `${header}.${payload}.sig`;
    localStorage.setItem('rememberMe', 'true');
    localStorage.setItem('accessToken', token);

    const { container, authService } = await setup();
    // Espionar logout sem executar a navegação para evitar problemas de teardown do Popover
    const espiao = vi.spyOn(authService, 'logout').mockImplementation(() => { /* noop para evitar teardown do Popover */ });

    // Abrir o menu do usuário
    const menuTrigger = container.querySelector('[data-cy="user-menu-trigger"]') as HTMLButtonElement;
    expect(menuTrigger).toBeTruthy();
    fireEvent.click(menuTrigger);

    // Clicar no botão "Sair" dentro do popover
    const botaoSair = document.querySelector('[data-cy="menu-sair"]') as HTMLButtonElement;
    expect(botaoSair).toBeTruthy();

    // Act
    fireEvent.click(botaoSair);

    // Assert
    expect(espiao).toHaveBeenCalledTimes(1);
  });

  it('Given_UsuarioAutenticado_Should_ExibirNomeDoUsuario', async () => {
    // Arrange — simular token válido no storage
    const header = btoa(JSON.stringify({ alg: 'HS256' }));
    const payload = btoa(JSON.stringify({ sub: '1', email: 'user@test.com', name: 'João Silva', exp: Math.floor(Date.now() / 1000) + 3600 }));
    const token = `${header}.${payload}.sig`;
    localStorage.setItem('rememberMe', 'true');
    localStorage.setItem('accessToken', token);

    const { container } = await setup();

    // Assert
    expect(container.querySelector('[data-cy="user-name"]')).toBeTruthy();
    expect(screen.getByText('João Silva')).toBeTruthy();
  });

  it('Given_UsuarioNaoAutenticado_Should_NaoExibirNomeDoUsuario', async () => {
    // Arrange — sem token
    const { container } = await setup();

    // Assert
    expect(container.querySelector('[data-cy="user-name"]')).toBeNull();
  });

  it('Given_TemaClaro_Should_ExibirAriaLabelAtivarTemaEscuro', async () => {
    // Arrange
    const { container, layoutService } = await setup();
    // Garantir tema claro
    if (layoutService.isDarkTheme()) {
      layoutService.toggleDarkMode();
    }

    // Act — re-renderizar
    const botoesDarkMode = container.querySelectorAll('.layout-config-menu button');
    let botaoDarkMode: HTMLButtonElement | null = null;
    botoesDarkMode.forEach(btn => {
      const ariaLabel = btn.getAttribute('aria-label') ?? '';
      if (ariaLabel.includes('tema')) {
        botaoDarkMode = btn as HTMLButtonElement;
      }
    });

    // Assert
    expect(botaoDarkMode!.getAttribute('aria-label')).toBe('Ativar tema escuro');
    expect(botaoDarkMode!.getAttribute('aria-pressed')).toBe('false');
  });

  it('Given_UsuarioAutenticado_Should_ExibirBotaoMenuUsuario', async () => {
    // Arrange
    const header = btoa(JSON.stringify({ alg: 'HS256' }));
    const payload = btoa(JSON.stringify({ sub: '1', email: 'user@test.com', name: 'João Silva', exp: Math.floor(Date.now() / 1000) + 3600 }));
    const token = `${header}.${payload}.sig`;
    localStorage.setItem('rememberMe', 'true');
    localStorage.setItem('accessToken', token);

    const { container } = await setup();

    // Assert
    const menuTrigger = container.querySelector('[data-cy="user-menu-trigger"]') as HTMLButtonElement;
    expect(menuTrigger).toBeTruthy();
    expect(menuTrigger.getAttribute('aria-label')).toBe('Menu do usuário');
  });

  it('Given_CliqueMenuUsuario_Should_ExibirLinkMeuPerfil', async () => {
    // Arrange
    const header = btoa(JSON.stringify({ alg: 'HS256' }));
    const payload = btoa(JSON.stringify({ sub: '1', email: 'user@test.com', name: 'João Silva', exp: Math.floor(Date.now() / 1000) + 3600 }));
    const token = `${header}.${payload}.sig`;
    localStorage.setItem('rememberMe', 'true');
    localStorage.setItem('accessToken', token);

    const { container } = await setup();

    // Abrir o menu
    const menuTrigger = container.querySelector('[data-cy="user-menu-trigger"]') as HTMLButtonElement;
    fireEvent.click(menuTrigger);

    // Assert
    const linkPerfil = document.querySelector('[data-cy="menu-meu-perfil"]') as HTMLAnchorElement;
    expect(linkPerfil).toBeTruthy();
    expect(linkPerfil.textContent).toContain('Meu Perfil');
  });

  it('Given_UsuarioNaoAutenticado_Should_NaoExibirBotaoMenuUsuario', async () => {
    // Arrange — sem token
    const { container } = await setup();

    // Assert
    expect(container.querySelector('[data-cy="user-menu-trigger"]')).toBeNull();
  });
});
