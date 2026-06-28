import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  auth.initializeFromStorage();

  if (!auth.isAuthenticated()) {
    return router.createUrlTree(['/login']);
  }

  const requiredRoles = route.data['roles'] as string[] | undefined;
  if (requiredRoles?.length && !auth.hasAnyRole(requiredRoles)) {
    return router.createUrlTree([auth.getDefaultRoute()]);
  }

  return true;
};

export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  auth.initializeFromStorage();

  if (auth.isAuthenticated()) {
    return router.createUrlTree([auth.getDefaultRoute()]);
  }

  return true;
};
