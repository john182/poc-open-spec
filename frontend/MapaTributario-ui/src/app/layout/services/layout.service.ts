import { Injectable, signal, computed, effect, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser, DOCUMENT } from '@angular/common';

export interface LayoutConfig {
  darkTheme: boolean;
  menuMode: 'static' | 'overlay';
}

interface LayoutState {
  staticMenuDesktopInactive: boolean;
  overlayMenuActive: boolean;
  mobileMenuActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class LayoutService {
  private _platformId = inject(PLATFORM_ID);
  private _document = inject(DOCUMENT);

  layoutConfig = signal<LayoutConfig>({
    darkTheme: this._loadDarkTheme(),
    menuMode: 'static',
  });

  layoutState = signal<LayoutState>({
    staticMenuDesktopInactive: false,
    overlayMenuActive: false,
    mobileMenuActive: false,
  });

  isDarkTheme = computed(() => this.layoutConfig().darkTheme);
  isOverlay = computed(() => this.layoutConfig().menuMode === 'overlay');
  isSidebarActive = computed(
    () => this.layoutState().overlayMenuActive || this.layoutState().mobileMenuActive
  );

  constructor() {
    // Apply saved dark mode on startup
    if (isPlatformBrowser(this._platformId)) {
      this.applyDarkMode(this.layoutConfig().darkTheme);
    }

    effect(() => {
      const config = this.layoutConfig();
      this.applyDarkMode(config.darkTheme);
      this._saveDarkTheme(config.darkTheme);
    });
  }

  onMenuToggle(): void {
    if (this.isOverlay()) {
      this.layoutState.update((s) => ({ ...s, overlayMenuActive: !s.overlayMenuActive }));
    } else if (this.isDesktop()) {
      this.layoutState.update((s) => ({
        ...s,
        staticMenuDesktopInactive: !s.staticMenuDesktopInactive,
      }));
    } else {
      this.layoutState.update((s) => ({ ...s, mobileMenuActive: !s.mobileMenuActive }));
    }
  }

  toggleDarkMode(): void {
    this.layoutConfig.update((c) => ({ ...c, darkTheme: !c.darkTheme }));
  }

  hideSidebar(): void {
    this.layoutState.update((s) => ({
      ...s,
      overlayMenuActive: false,
      mobileMenuActive: false,
    }));
  }

  isDesktop(): boolean {
    if (!isPlatformBrowser(this._platformId)) {
      return true;
    }
    return window.innerWidth > 991;
  }

  private applyDarkMode(dark: boolean): void {
    if (!isPlatformBrowser(this._platformId)) {
      return;
    }
    if (dark) {
      this._document.documentElement.classList.add('app-dark');
    } else {
      this._document.documentElement.classList.remove('app-dark');
    }
  }

  private _loadDarkTheme(): boolean {
    if (!isPlatformBrowser(this._platformId)) {
      return false;
    }
    return localStorage.getItem('darkTheme') === 'true';
  }

  private _saveDarkTheme(dark: boolean): void {
    if (isPlatformBrowser(this._platformId)) {
      localStorage.setItem('darkTheme', String(dark));
    }
  }
}
