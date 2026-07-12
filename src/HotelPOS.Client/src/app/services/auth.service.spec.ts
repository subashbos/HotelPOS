import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AuthService } from './auth.service';
import { environment } from '../../environments/environment';

const ROLE_CLAIM_TYPE = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

function buildToken(payload: Record<string, unknown>): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(JSON.stringify(payload));
  return `${header}.${body}.signature`;
}

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
    localStorage.clear();
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('reads role from the JWT claims rather than a separate localStorage key', () => {
    localStorage.setItem('auth_token', buildToken({ [ROLE_CLAIM_TYPE]: 'Admin' }));

    expect(service.getRole()).toBe('Admin');
    expect(localStorage.getItem('auth_role')).toBeNull();
  });

  it('ignores a spoofed auth_role value edited directly in localStorage', () => {
    localStorage.setItem('auth_token', buildToken({ [ROLE_CLAIM_TYPE]: 'Cashier' }));
    localStorage.setItem('auth_role', 'Admin'); // e.g. an attacker editing devtools storage

    expect(service.getRole()).toBe('Cashier');
  });

  it('returns null role when there is no token', () => {
    expect(service.getRole()).toBeNull();
  });

  it('keeps the refresh token in memory only after login, not in localStorage', () => {
    service.login({ username: 'u', password: 'p' }).subscribe();

    httpMock.expectOne(`${environment.apiBaseUrl}/auth/login`).flush({
      token: buildToken({ [ROLE_CLAIM_TYPE]: 'Admin' }),
      refreshToken: 'refresh-abc',
      username: 'u',
      role: 'Admin'
    });

    expect(service.hasRefreshToken()).toBeTrue();
    expect(service.getToken()).not.toBeNull();
    expect(Object.keys(localStorage).some(k => k.toLowerCase().includes('refresh'))).toBeFalse();
  });

  it('sends the in-memory refresh token as the body of refresh()', () => {
    service.login({ username: 'u', password: 'p' }).subscribe();
    httpMock.expectOne(`${environment.apiBaseUrl}/auth/login`).flush({
      token: buildToken({ [ROLE_CLAIM_TYPE]: 'Admin' }),
      refreshToken: 'refresh-abc',
      username: 'u',
      role: 'Admin'
    });

    service.refresh().subscribe();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/auth/refresh`);
    expect(req.request.body.refreshToken).toBe('refresh-abc');
    req.flush({
      token: buildToken({ [ROLE_CLAIM_TYPE]: 'Admin' }),
      refreshToken: 'refresh-def',
      username: 'u',
      role: 'Admin'
    });
  });

  it('clears the session and best-effort revokes the refresh token on logout', () => {
    service.login({ username: 'u', password: 'p' }).subscribe();
    httpMock.expectOne(`${environment.apiBaseUrl}/auth/login`).flush({
      token: buildToken({ [ROLE_CLAIM_TYPE]: 'Admin' }),
      refreshToken: 'refresh-abc',
      username: 'u',
      role: 'Admin'
    });

    service.logout();

    expect(service.getToken()).toBeNull();
    expect(service.hasRefreshToken()).toBeFalse();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/auth/logout`);
    expect(req.request.body.refreshToken).toBe('refresh-abc');
    req.flush({});
  });
});
