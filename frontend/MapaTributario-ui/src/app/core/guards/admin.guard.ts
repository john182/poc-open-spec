import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { RoleService } from '../auth/role.service';

export const adminGuard: CanActivateFn = () => {
  const platformId = inject(PLATFORM_ID);
  const router = inject(Router);

  // No servidor, redirecionar para login — sem acesso a storage não há como validar token
  if (!isPlatformBrowser(platformId)) {
    return router.createUrlTree(['/auth/login']);
  }

  const authService = inject(AuthService);
  const roleService = inject(RoleService);

  if (!authService.isAuthenticated()) {
    return router.createUrlTree(['/auth/login']);
  }

  if (!roleService.isAdmin()) {
    return router.createUrlTree(['/acesso-negado']);
  }

  return true;
};
