import { Injectable, PLATFORM_ID, inject, computed } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { AuthService } from './auth.service';

export type RoleUsuario = 'Admin' | 'User' | null;

@Injectable({ providedIn: 'root' })
export class RoleService {
  private readonly _authService = inject(AuthService);
  private readonly _platformId = inject(PLATFORM_ID);

  readonly role = computed<RoleUsuario>(() => {
    if (!this._authService.isAuthenticated()) return null;
    return this._extrairRole();
  });

  readonly isAdmin = computed(() => this.role() === 'Admin');

  private _extrairRole(): RoleUsuario {
    if (!isPlatformBrowser(this._platformId)) return null;

    const token = this._authService.getAccessToken();
    if (!token) return null;

    try {
      const parts = token.split('.');
      if (parts.length !== 3) return null;
      const payload = JSON.parse(atob(parts[1]));

      const role =
        payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ??
        payload.role ??
        null;

      if (role === 'Admin') return 'Admin';
      return 'User';
    } catch {
      return null;
    }
  }
}
