import { Component } from '@angular/core';
import { NgClass } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';

export interface AppMenuItem {
  label: string;
  icon?: string;
  routerLink?: string[];
  items?: AppMenuItem[];
}

@Component({
  selector: 'app-menu',
  standalone: true,
  imports: [NgClass, RouterLink, RouterLinkActive],
  templateUrl: './app-menu.component.html',
  styleUrl: './app-menu.component.scss',
})
export class AppMenuComponent {
  model: AppMenuItem[] = [
    {
      label: 'Menu',
      items: [
        { label: 'Consulta de Alíquotas', icon: 'pi pi-map', routerLink: ['/consulta'] },
      ],
    },
  ];
}
