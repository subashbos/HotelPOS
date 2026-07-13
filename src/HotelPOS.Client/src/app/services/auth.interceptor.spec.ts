import { HttpHandler, HttpRequest, HttpErrorResponse, HttpResponse, HttpEvent } from '@angular/common/http';
import { of, throwError, Subject } from 'rxjs';
import { AuthInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';
import { Router } from '@angular/router';

describe('AuthInterceptor', () => {
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let routerSpy: jasmine.SpyObj<Router>;
  let interceptor: AuthInterceptor;

  beforeEach(() => {
    authServiceSpy = jasmine.createSpyObj('AuthService', ['getToken', 'logout', 'hasRefreshToken', 'refresh']);
    routerSpy = jasmine.createSpyObj('Router', ['navigate']);
    interceptor = new AuthInterceptor(authServiceSpy, routerSpy);
  });

  function makeHandler(response$: ReturnType<typeof of> | ReturnType<typeof throwError>): HttpHandler {
    return { handle: () => response$ } as unknown as HttpHandler;
  }

  it('attaches the bearer token when one is present', () => {
    authServiceSpy.getToken.and.returnValue('abc123');
    const req = new HttpRequest('GET', '/api/items');
    let capturedRequest!: HttpRequest<unknown>;
    const handler = { handle: (r: HttpRequest<unknown>) => { capturedRequest = r; return of(new HttpResponse()); } } as HttpHandler;

    interceptor.intercept(req, handler).subscribe();

    expect(capturedRequest.headers.get('Authorization')).toBe('Bearer abc123');
  });

  it('does not attach an Authorization header when there is no token', () => {
    authServiceSpy.getToken.and.returnValue(null);
    const req = new HttpRequest('GET', '/api/items');
    let capturedRequest!: HttpRequest<unknown>;
    const handler = { handle: (r: HttpRequest<unknown>) => { capturedRequest = r; return of(new HttpResponse()); } } as HttpHandler;

    interceptor.intercept(req, handler).subscribe();

    expect(capturedRequest.headers.has('Authorization')).toBeFalse();
  });

  it('logs out and redirects to /login on a 401 with no refresh token available', () => {
    authServiceSpy.getToken.and.returnValue('expired-token');
    authServiceSpy.hasRefreshToken.and.returnValue(false);
    const req = new HttpRequest('GET', '/api/orders');
    const handler = makeHandler(throwError(() => new HttpErrorResponse({ status: 401 })));

    interceptor.intercept(req, handler).subscribe({ error: () => {} });

    expect(authServiceSpy.logout).toHaveBeenCalled();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('does not redirect on a 401 from the login endpoint itself', () => {
    authServiceSpy.getToken.and.returnValue(null);
    const req = new HttpRequest('POST', '/api/auth/login', {});
    const handler = makeHandler(throwError(() => new HttpErrorResponse({ status: 401 })));

    interceptor.intercept(req, handler).subscribe({ error: () => {} });

    expect(authServiceSpy.logout).not.toHaveBeenCalled();
    expect(routerSpy.navigate).not.toHaveBeenCalled();
  });

  it('propagates non-401 errors without touching the session', () => {
    authServiceSpy.getToken.and.returnValue('abc123');
    const req = new HttpRequest('GET', '/api/orders');
    const handler = makeHandler(throwError(() => new HttpErrorResponse({ status: 500 })));

    let caught: HttpErrorResponse | undefined;
    interceptor.intercept(req, handler).subscribe({ error: (e) => { caught = e; } });

    expect(caught?.status).toBe(500);
    expect(authServiceSpy.logout).not.toHaveBeenCalled();
    expect(routerSpy.navigate).not.toHaveBeenCalled();
  });

  it('refreshes the token and retries the original request on a 401', () => {
    authServiceSpy.getToken.and.returnValue('expired-token');
    authServiceSpy.hasRefreshToken.and.returnValue(true);
    authServiceSpy.refresh.and.returnValue(of({ token: 'new-token', refreshToken: 'new-refresh', username: 'u', role: 'Admin' }));

    const req = new HttpRequest('GET', '/api/orders');
    let attempt = 0;
    let retriedRequest!: HttpRequest<unknown>;
    const handler = {
      handle: (r: HttpRequest<unknown>) => {
        attempt++;
        if (attempt === 1) {
          return throwError(() => new HttpErrorResponse({ status: 401 }));
        }
        retriedRequest = r;
        return of(new HttpResponse({ status: 200 }));
      }
    } as HttpHandler;

    let result: HttpEvent<unknown> | undefined;
    interceptor.intercept(req, handler).subscribe(event => { result = event; });

    expect(authServiceSpy.refresh).toHaveBeenCalled();
    expect(retriedRequest.headers.get('Authorization')).toBe('Bearer new-token');
    expect((result as HttpResponse<unknown>).status).toBe(200);
    expect(authServiceSpy.logout).not.toHaveBeenCalled();
  });

  it('logs out when the refresh call itself fails', () => {
    authServiceSpy.getToken.and.returnValue('expired-token');
    authServiceSpy.hasRefreshToken.and.returnValue(true);
    authServiceSpy.refresh.and.returnValue(throwError(() => new HttpErrorResponse({ status: 401 })));

    const req = new HttpRequest('GET', '/api/orders');
    const handler = makeHandler(throwError(() => new HttpErrorResponse({ status: 401 })));

    interceptor.intercept(req, handler).subscribe({ error: () => {} });

    expect(authServiceSpy.logout).toHaveBeenCalled();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('logs out instead of refreshing again if the retried request still gets a 401', () => {
    authServiceSpy.getToken.and.returnValue('expired-token');
    authServiceSpy.hasRefreshToken.and.returnValue(true);
    authServiceSpy.refresh.and.returnValue(of({ token: 'new-token', refreshToken: 'new-refresh', username: 'u', role: 'Admin' }));

    const req = new HttpRequest('GET', '/api/orders');
    const handler = makeHandler(throwError(() => new HttpErrorResponse({ status: 401 })));

    interceptor.intercept(req, handler).subscribe({ error: () => {} });

    expect(authServiceSpy.refresh).toHaveBeenCalledTimes(1);
    expect(authServiceSpy.logout).toHaveBeenCalled();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('queues request when token refresh is in flight', () => {
    authServiceSpy.getToken.and.returnValue('expired-token');
    authServiceSpy.hasRefreshToken.and.returnValue(true);

    const refreshSubject = new Subject<any>();
    authServiceSpy.refresh.and.returnValue(refreshSubject);

    const req1 = new HttpRequest('GET', '/api/orders/1');
    const req2 = new HttpRequest('GET', '/api/orders/2');

    let req1Attempts = 0;
    let req2Attempts = 0;
    let req1Retried = false;
    let req2Retried = false;

    const handler1 = {
      handle: (r: HttpRequest<unknown>) => {
        req1Attempts++;
        if (req1Attempts === 1) {
          return throwError(() => new HttpErrorResponse({ status: 401 }));
        }
        req1Retried = true;
        return of(new HttpResponse({ status: 200 }));
      }
    } as HttpHandler;

    const handler2 = {
      handle: (r: HttpRequest<unknown>) => {
        req2Attempts++;
        if (req2Attempts === 1) {
          return throwError(() => new HttpErrorResponse({ status: 401 }));
        }
        req2Retried = true;
        return of(new HttpResponse({ status: 200 }));
      }
    } as HttpHandler;

    interceptor.intercept(req1, handler1).subscribe();
    interceptor.intercept(req2, handler2).subscribe();

    expect(req1Retried).toBeFalse();
    expect(req2Retried).toBeFalse();

    refreshSubject.next({ token: 'new-token', refreshToken: 'new-refresh', username: 'u', role: 'Admin' });
    refreshSubject.complete();

    expect(req1Retried).toBeTrue();
    expect(req2Retried).toBeTrue();
  });
});
