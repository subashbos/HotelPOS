import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, shareReplay, tap } from 'rxjs';
import { environment } from '../../environments/environment';

/**
 * Caches the current user's effective access per permission module, fetched from
 * GET /api/auth/permissions - the same IAuthorizationService.HasPermission check the backend
 * uses to enforce every API endpoint. Route guards and nav visibility read from this cache
 * instead of duplicating role logic client-side, so they can never grant access the backend
 * would actually reject.
 */
@Injectable({
  providedIn: 'root'
})
export class PermissionService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiBaseUrl}/auth/permissions`;

  private readonly permissionsSignal = signal<Record<string, boolean> | null>(null);
  private pending$: Observable<Record<string, boolean>> | null = null;

  /** Fetches the permission map on first call (or after clear()); subsequent calls reuse the cached snapshot. */
  ensureLoaded(): Observable<Record<string, boolean>> {
    const cached = this.permissionsSignal();
    if (cached) {
      return of(cached);
    }

    if (!this.pending$) {
      this.pending$ = this.http.get<Record<string, boolean>>(this.apiUrl).pipe(
        tap(permissions => {
          this.permissionsSignal.set(permissions);
          this.pending$ = null;
        }),
        shareReplay(1)
      );
    }
    return this.pending$;
  }

  /** Synchronous check against the last-loaded snapshot. Denies (false) until a snapshot has loaded. */
  hasPermission(module: string): boolean {
    return this.permissionsSignal()?.[module] ?? false;
  }

  /** True once a permission snapshot has been loaded - lets templates avoid a flash of hidden nav items. */
  isLoaded(): boolean {
    return this.permissionsSignal() !== null;
  }

  /** Drops the cached snapshot; call on logout so the next login re-fetches fresh permissions for that user. */
  clear(): void {
    this.permissionsSignal.set(null);
    this.pending$ = null;
  }
}
