import { Component, computed, inject } from '@angular/core';
import { NgClass } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { RoleService } from '../../core/auth/role.service';

export interface AppMenuItem {
  label: string;
  icon?: string;
  routerLink?: string[];
  items?: AppMenuItem[];
  adminOnly?: boolean;
}

@Component({
  selector: 'app-menu',
  standalone: true,
  imports: [NgClass, RouterLink, RouterLinkActive],
  templateUrl: './app-menu.component.html',
  styleUrl: './app-menu.component.scss',
})
export class AppMenuComponent {
  private readonly _roleService = inject(RoleService);

  private readonly _todosMenus: AppMenuItem[] = [
    {
      label: 'Menu',
      items: [
        { label: 'Consulta de Alíquotas', icon: 'pi pi-map', routerLink: ['/consulta'] },
      ],
    },
    {
      label: 'Administração',
      adminOnly: true,
      items: [
        { label: 'Crawler', icon: 'pi pi-cog', routerLink: ['/admin/crawler/status'] },
        { label: 'Certificado', icon: 'pi pi-shield', routerLink: ['/admin/crawler/certificado'] },
        { label: 'Execuções', icon: 'pi pi-history', routerLink: ['/admin/crawler/execucoes'] },
        { label: 'Configuração', icon: 'pi pi-sliders-h', routerLink: ['/admin/crawler/configuracao'] },
      ],
    },
  ];

  readonly model = computed(() => {
    const isAdmin = this._roleService.isAdmin();
    return this._todosMenus
      .filter(grupo => !grupo.adminOnly || isAdmin)
      .map(grupo => ({
        ...grupo,
        items: grupo.items?.filter(item => !item.adminOnly || isAdmin),
      }));
  });
}
