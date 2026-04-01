import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { LayoutService } from './layout.service';

describe('LayoutService', () => {
  let service: LayoutService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(LayoutService);
  });

  it('deve ser criado', () => {
    expect(service).toBeTruthy();
  });

  it('deve iniciar com tema claro', () => {
    expect(service.isDarkTheme()).toBe(false);
  });

  it('deve iniciar com menu static', () => {
    expect(service.isOverlay()).toBe(false);
  });

  it('deve alternar dark mode', () => {
    service.toggleDarkMode();
    expect(service.isDarkTheme()).toBe(true);
    service.toggleDarkMode();
    expect(service.isDarkTheme()).toBe(false);
  });

  it('deve fechar menu mobile', () => {
    service.layoutState.update((s) => ({ ...s, mobileMenuActive: true }));
    expect(service.isSidebarActive()).toBe(true);
    service.hideSidebar();
    expect(service.layoutState().mobileMenuActive).toBe(false);
    expect(service.layoutState().overlayMenuActive).toBe(false);
  });

  it('deve toggle menu overlay', () => {
    service.layoutConfig.update((c) => ({ ...c, menuMode: 'overlay' }));
    service.onMenuToggle();
    expect(service.layoutState().overlayMenuActive).toBe(true);
    service.onMenuToggle();
    expect(service.layoutState().overlayMenuActive).toBe(false);
  });

  it('deve toggle menu static desktop', () => {
    // Simular desktop (largura > 991)
    vi.spyOn(service, 'isDesktop').mockReturnValue(true);
    service.onMenuToggle();
    expect(service.layoutState().staticMenuDesktopInactive).toBe(true);
    service.onMenuToggle();
    expect(service.layoutState().staticMenuDesktopInactive).toBe(false);
  });

  it('deve toggle menu mobile quando nao desktop', () => {
    vi.spyOn(service, 'isDesktop').mockReturnValue(false);
    service.onMenuToggle();
    expect(service.layoutState().mobileMenuActive).toBe(true);
  });

  it('isDesktop retorna boolean baseado em window.innerWidth', () => {
    const result = service.isDesktop();
    expect(typeof result).toBe('boolean');
  });

  it('isSidebarActive quando overlay ativo', () => {
    service.layoutState.update((s) => ({ ...s, overlayMenuActive: true }));
    expect(service.isSidebarActive()).toBe(true);
  });

  it('hideSidebar deve fechar overlay e mobile', () => {
    service.layoutState.update((s) => ({ ...s, overlayMenuActive: true, mobileMenuActive: true }));
    service.hideSidebar();
    expect(service.layoutState().overlayMenuActive).toBe(false);
    expect(service.layoutState().mobileMenuActive).toBe(false);
  });

  it('isOverlay deve retornar true quando menuMode overlay', () => {
    service.layoutConfig.update((c) => ({ ...c, menuMode: 'overlay' }));
    expect(service.isOverlay()).toBe(true);
  });
});
