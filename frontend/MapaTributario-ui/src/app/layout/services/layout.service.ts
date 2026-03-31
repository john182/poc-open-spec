import { Injectable, signal, computed, effect } from '@angular/core';

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
  layoutConfig = signal<LayoutConfig>({
    darkTheme: false,
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

  private _initialized = false;

  constructor() {
    effect(() => {
      const config = this.layoutConfig();
      if (!this._initialized) {
        this._initialized = true;
        return;
      }
      this.applyDarkMode(config.darkTheme);
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

  closeMobileMenu(): void {
    this.layoutState.update((s) => ({ ...s, mobileMenuActive: false }));
  }

  isDesktop(): boolean {
    return window.innerWidth > 991;
  }

  private applyDarkMode(dark: boolean): void {
    if (dark) {
      document.documentElement.classList.add('app-dark');
    } else {
      document.documentElement.classList.remove('app-dark');
    }
  }
}
