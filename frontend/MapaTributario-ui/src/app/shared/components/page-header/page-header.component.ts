import { Component, input } from '@angular/core';
import { BreadcrumbModule } from 'primeng/breadcrumb';
import { MenuItem } from 'primeng/api';

@Component({
  selector: 'app-page-header',
  standalone: true,
  imports: [BreadcrumbModule],
  templateUrl: './page-header.component.html',
  styleUrl: './page-header.component.scss',
})
export class PageHeaderComponent {
  titulo = input.required<string>();
  migalhas = input<MenuItem[]>([]);
  home: MenuItem = { icon: 'pi pi-home', routerLink: '/' };
}
