import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, BehaviorSubject, catchError, filter, switchMap, take, throwError } from 'rxjs';
import { AuthService } from './auth.service';

const RETRY_HEADER = 'X-Retried-After-Refresh';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private isRefreshing = false;
  private refreshedToken$ = new BehaviorSubject<string | null>(null);

  constructor(private readonly authService: AuthService, private readonly router: Router) {}

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const token = this.authService.getToken();
    const cloned = token ? this.attachToken(req, token) : req;

    return next.handle(cloned).pipe(
      catchError((error: HttpErrorResponse) => this.handleError(error, req, next))
    );
  }

  private handleError(error: HttpErrorResponse, req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const isAuthEndpoint = ['/auth/login', '/auth/refresh', '/auth/logout'].some(path => req.url.includes(path));

    // 401s on the auth endpoints themselves mean bad credentials or an expired/invalid
    // refresh token — never worth another refresh attempt.
    if (error.status !== 401 || isAuthEndpoint) {
      return throwError(() => error);
    }

    // Already went through one refresh-and-retry cycle for this request; a second
    // 401 means the new token is no good either, so stop instead of looping.
    const alreadyRetried = req.headers.has(RETRY_HEADER);
    if (alreadyRetried || !this.authService.hasRefreshToken()) {
      this.sessionExpired();
      return throwError(() => error);
    }

    return this.refreshAndRetry(req, next);
  }

  private refreshAndRetry(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const retry = (token: string): Observable<HttpEvent<unknown>> => {
      const retried = this.attachToken(req, token, true);
      return next.handle(retried).pipe(
        catchError((error: HttpErrorResponse) => this.handleError(error, retried, next))
      );
    };

    if (this.isRefreshing) {
      return this.refreshedToken$.pipe(
        filter((token): token is string => token !== null),
        take(1),
        switchMap(token => retry(token))
      );
    }

    this.isRefreshing = true;
    this.refreshedToken$.next(null);

    return this.authService.refresh().pipe(
      switchMap(response => {
        this.isRefreshing = false;
        this.refreshedToken$.next(response.token);
        return retry(response.token);
      }),
      catchError(refreshError => {
        this.isRefreshing = false;
        this.sessionExpired();
        return throwError(() => refreshError);
      })
    );
  }

  private attachToken(req: HttpRequest<unknown>, token: string, markRetried = false): HttpRequest<unknown> {
    const headers: Record<string, string> = { Authorization: `Bearer ${token}` };
    if (markRetried) headers[RETRY_HEADER] = 'true';
    return req.clone({ setHeaders: headers });
  }

  private sessionExpired(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
