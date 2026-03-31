import { Component, input } from '@angular/core';
import { BreadcrumbModule } from 'primeng/breadcrumb';
import { MenuItem } from 'primeng/api';

@Component({
  selector: 'app-page-header',
  standalone: true,
  imports: [BreadcrumbModule],
  template: `
    <div class="page-header" data-cy="page-header">
      <h1>{{ titulo() }}</h1>
      @if (migalhas().length > 0) {
        <p-breadcrumb [model]="migalhas()" [home]="home" />
      }
    </div>
  `,
  styles: [`
    .page-header { margin-bottom: 1.5rem; }
    h1 { font-size: 1.5rem; font-weight: 700; color: var(--color-text-primary); margin: 0 0 0.5rem; }
  `],
})
export class PageHeaderComponent {
  titulo = input.required<string>();
  migalhas = input<MenuItem[]>([]);
  home: MenuItem = { icon: 'pi pi-home', routerLink: '/' };
}
