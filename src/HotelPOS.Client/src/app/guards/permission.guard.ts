import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { PermissionService } from '../services/permission.service';

/**
 * Gates a route on one or more permission modules declared via route `data: { modules: [...] }`.
 * A route with no `modules` entry (or an empty array) has no permission requirement beyond being
 * logged in (authGuard already covers that) - e.g. Billing POS, Shift Session, Account.
 * When multiple modules are listed, access is granted if the user holds ANY of them (OR
 * semantics), matching the WPF client's `HasPermission(A) || HasPermission(B)` pattern used for
 * pages fed by more than one module (e.g. the purchase report, visible via Purchase or SalesReport).
 */
export const permissionGuard: CanActivateFn = (route) => {
  const permissionService = inject(PermissionService);
  const router = inject(Router);

  const requiredModules = route.data?.['modules'] as string[] | undefined;
  if (!requiredModules || requiredModules.length === 0) {
    return true;
  }

  return permissionService.ensureLoaded().pipe(
    map(() => {
      const allowed = requiredModules.some(module => permissionService.hasPermission(module));
      if (!allowed) {
        router.navigate(['/admin/forbidden']);
      }
      return allowed;
    })
  );
};
