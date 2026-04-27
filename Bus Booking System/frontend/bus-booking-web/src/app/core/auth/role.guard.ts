import { CanMatchFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthStore } from './auth.store';

export function roleGuard(allowedRoles: string[]): CanMatchFn {
  return () => {
    const auth = inject(AuthStore);
    const router = inject(Router);

    if (!auth.isLoggedIn()) {
      return router.createUrlTree(['/login']);
    }
    const roles = auth.roles();
    const allowed = allowedRoles.some(r => roles.includes(r));
    if (!allowed) {
      return router.createUrlTree(['/']);
    }
    return true;
  };
}
