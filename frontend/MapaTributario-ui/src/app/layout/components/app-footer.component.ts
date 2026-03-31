import { Component } from '@angular/core';

@Component({
  selector: 'app-footer',
  standalone: true,
  template: `
    <div class="layout-footer">
      <span>Mapa Tributário © 2026</span>
    </div>
  `,
  styles: [`
    .layout-footer {
      padding: 1rem 1.5rem;
      text-align: center;
      font-size: 0.875rem;
      color: var(--color-text-muted);
      border-top: 1px solid var(--border-color);
    }
  `],
})
export class AppFooterComponent {}
