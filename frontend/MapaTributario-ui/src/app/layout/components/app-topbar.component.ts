import { Component, inject, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Popover, PopoverModule } from 'primeng/popover';
import { ButtonModule } from 'primeng/button';
import { LayoutService } from '../services/layout.service';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [CommonModule, RouterModule, PopoverModule, ButtonModule],
  templateUrl: './app-topbar.component.html',
  styleUrl: './app-topbar.component.scss',
})
export class AppTopbarComponent {
  layoutService = inject(LayoutService);
  authService = inject(AuthService);

  @ViewChild('menuUsuario') menuUsuario!: Popover;

  toggleMenuUsuario(evento: Event): void {
    this.menuUsuario.toggle(evento);
  }

  fecharMenuUsuario(): void {
    this.menuUsuario.hide();
  }

  toggleDarkMode(): void {
    this.layoutService.toggleDarkMode();
  }

  logout(): void {
    this.fecharMenuUsuario();
    this.authService.logout();
  }
}
