import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AppTopbarComponent } from './app-topbar.component';
import { AppSidebarComponent } from './app-sidebar.component';
import { AppFooterComponent } from './app-footer.component';
import { LayoutService } from '../services/layout.service';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [RouterOutlet, AppTopbarComponent, AppSidebarComponent, AppFooterComponent],
  template: `
    <div
      class="layout-wrapper"
      [class.layout-static]="!layoutService.isOverlay()"
      [class.layout-overlay]="layoutService.isOverlay()"
      [class.layout-static-inactive]="layoutService.layoutState().staticMenuDesktopInactive"
      [class.layout-mobile-active]="layoutService.layoutState().mobileMenuActive"
    >
      <app-topbar />
      <app-sidebar />
      <div class="layout-main">
        <div class="layout-content">
          <router-outlet />
        </div>
        <app-footer />
      </div>
      @if (layoutService.isSidebarActive()) {
        <div class="layout-mask" (click)="layoutService.closeMobileMenu()"></div>
      }
    </div>
  `,
  styles: [`
    .layout-wrapper { min-height: 100vh; }
    .layout-main {
      margin-left: 0;
      padding-top: 4rem;
      transition: margin-left 0.3s;
    }
    .layout-static .layout-main { margin-left: 16rem; }
    .layout-static.layout-static-inactive .layout-main { margin-left: 0; }
    .layout-content { padding: 1.5rem; }
    .layout-mask {
      position: fixed;
      inset: 0;
      background: rgba(0,0,0,0.4);
      z-index: 998;
    }
    @media (max-width: 991px) {
      .layout-static .layout-main { margin-left: 0; }
    }
  `],
})
export class AppLayoutComponent {
  layoutService = inject(LayoutService);
}
