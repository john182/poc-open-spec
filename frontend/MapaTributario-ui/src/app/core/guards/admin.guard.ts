import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { RoleService } from '../auth/role.service';

export const adminGuard: CanActivateFn = () => {
  const platformId = inject(PLATFORM_ID);

  if (!isPlatformBrowser(platformId)) {
    return true;
  }

  const authService = inject(AuthService);
  const roleService = inject(RoleService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    return router.createUrlTree(['/auth/login']);
  }

  if (!roleService.isAdmin()) {
    return router.createUrlTree(['/acesso-negado']);
  }

  return true;
};
