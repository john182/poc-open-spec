import { Component, inject } from '@angular/core';
import { AppMenuComponent } from './app-menu.component';
import { LayoutService } from '../services/layout.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [AppMenuComponent],
  template: `
    <div class="layout-sidebar" [class.active]="layoutService.isSidebarActive()">
      <app-menu />
    </div>
  `,
  styles: [`
    .layout-sidebar {
      position: fixed;
      left: 0;
      top: 4rem;
      bottom: 0;
      width: 16rem;
      background: var(--color-surface-0);
      border-right: 1px solid var(--border-color);
      overflow-y: auto;
      transition: transform 0.3s;
      z-index: 999;
    }
    @media (max-width: 991px) {
      .layout-sidebar {
        transform: translateX(-100%);
      }
      .layout-sidebar.active {
        transform: translateX(0);
      }
    }
  `],
})
export class AppSidebarComponent {
  layoutService = inject(LayoutService);
}
