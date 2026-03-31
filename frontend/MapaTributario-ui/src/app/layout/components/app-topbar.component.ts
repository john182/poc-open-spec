import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { LayoutService } from '../services/layout.service';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [RouterLink, ButtonModule],
  template: `
    <div class="layout-topbar">
      <a routerLink="/" class="layout-topbar-logo">
        <span class="layout-topbar-logo-text">Mapa Tributário</span>
      </a>
      <button
        class="layout-menu-button"
        (click)="layoutService.onMenuToggle()"
        pButton
        icon="pi pi-bars"
        [text]="true"
        [rounded]="true"
      ></button>
      <div class="layout-topbar-actions">
        <button
          (click)="layoutService.toggleDarkMode()"
          pButton
          [icon]="layoutService.isDarkTheme() ? 'pi pi-sun' : 'pi pi-moon'"
          [text]="true"
          [rounded]="true"
        ></button>
      </div>
    </div>
  `,
  styles: [`
    .layout-topbar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0 1.5rem;
      height: 4rem;
      background: var(--color-surface-0);
      border-bottom: 1px solid var(--border-color);
    }
    .layout-topbar-logo {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      text-decoration: none;
      color: var(--color-text-primary);
      font-weight: 700;
      font-size: 1.25rem;
    }
    .layout-topbar-actions {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }
  `],
})
export class AppTopbarComponent {
  layoutService = inject(LayoutService);
}
