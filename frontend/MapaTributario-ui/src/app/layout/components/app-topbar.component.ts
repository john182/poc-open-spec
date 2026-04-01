import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { LayoutService } from '../services/layout.service';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './app-topbar.component.html',
  styleUrl: './app-topbar.component.scss',
})
export class AppTopbarComponent {
  layoutService = inject(LayoutService);
  authService = inject(AuthService);

  toggleDarkMode(): void {
    this.layoutService.toggleDarkMode();
  }

  logout(): void {
    this.authService.logout();
  }
}
