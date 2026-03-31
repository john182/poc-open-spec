import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

export interface MenuItem {
  label: string;
  icon?: string;
  routerLink?: string[];
  items?: MenuItem[];
}

@Component({
  selector: 'app-menu',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  template: `
    <ul class="layout-menu">
      @for (item of model; track item.label) {
        <li class="layout-menu-section">
          <span class="layout-menu-section-label">{{ item.label }}</span>
          <ul>
            @for (child of item.items; track child.label) {
              <li>
                <a
                  [routerLink]="child.routerLink"
                  routerLinkActive="active-menuitem"
                  class="layout-menuitem"
                >
                  <i [class]="child.icon"></i>
                  <span>{{ child.label }}</span>
                </a>
              </li>
            }
          </ul>
        </li>
      }
    </ul>
  `,
  styles: [`
    .layout-menu {
      list-style: none;
      padding: 0.5rem;
      margin: 0;
    }
    .layout-menu-section-label {
      display: block;
      padding: 0.75rem 1rem 0.5rem;
      font-size: 0.75rem;
      font-weight: 600;
      text-transform: uppercase;
      color: var(--color-text-muted);
    }
    .layout-menuitem {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 0.75rem 1rem;
      border-radius: var(--border-radius);
      text-decoration: none;
      color: var(--color-text-secondary);
      transition: background 0.2s;
    }
    .layout-menuitem:hover {
      background: var(--color-surface-100);
    }
    .layout-menuitem.active-menuitem {
      background: var(--color-primary);
      color: var(--color-primary-text);
    }
  `],
})
export class AppMenuComponent {
  model: MenuItem[] = [
    {
      label: 'Menu',
      items: [
        { label: 'Consulta de Alíquotas', icon: 'pi pi-map', routerLink: ['/consulta'] },
      ],
    },
  ];
}
