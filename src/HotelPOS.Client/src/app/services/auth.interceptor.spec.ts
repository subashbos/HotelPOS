import { HttpHandler, HttpRequest, HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { AuthInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';
import { Router } from '@angular/router';

describe('AuthInterceptor', () => {
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let routerSpy: jasmine.SpyObj<Router>;
  let interceptor: AuthInterceptor;

  beforeEach(() => {
    authServiceSpy = jasmine.createSpyObj('AuthService', ['getToken', 'logout']);
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

  it('logs out and redirects to /login on a 401 from a non-login request', () => {
    authServiceSpy.getToken.and.returnValue('expired-token');
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
});
