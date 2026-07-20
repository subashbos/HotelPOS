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

  it('returns username from localStorage', () => {
    localStorage.setItem('auth_username', 'admin');
    expect(service.getUsername()).toBe('admin');
  });

  it('returns isLoggedIn status', () => {
    expect(service.isLoggedIn()).toBeFalse();
    localStorage.setItem('auth_token', 'token');
    expect(service.isLoggedIn()).toBeTrue();
  });

  it('handles decodeToken catching error for malformed token', () => {
    localStorage.setItem('auth_token', 'malformed-token');
    expect(service.getRole()).toBeNull();
  });

  describe('getUserId', () => {
    it('returns null if there is no token', () => {
      expect(service.getUserId()).toBeNull();
    });

    it('returns parsed integer user ID if sub claim is a valid numeric string', () => {
      localStorage.setItem('auth_token', buildToken({ sub: '123' }));
      expect(service.getUserId()).toBe(123);
    });

    it('returns null if sub claim is not a numeric string', () => {
      localStorage.setItem('auth_token', buildToken({ sub: 'abc' }));
      expect(service.getUserId()).toBeNull();
    });

    it('returns null if sub claim is missing', () => {
      localStorage.setItem('auth_token', buildToken({ other: 'value' }));
      expect(service.getUserId()).toBeNull();
    });
  });

  describe('logout without refresh token', () => {
    it('does not send logout API call if there is no refresh token in memory', () => {
      service.logout();
      expect(service.getToken()).toBeNull();
      expect(service.hasRefreshToken()).toBeFalse();
      httpMock.expectNone(`${environment.apiBaseUrl}/auth/logout`);
    });
  });

  describe('password reset flows', () => {
    it('sends POST request to forgot-password endpoint', () => {
      service.forgotPassword('user1').subscribe();
      const req = httpMock.expectOne(`${environment.apiBaseUrl}/auth/forgot-password`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ username: 'user1' });
      req.flush({});
    });

    it('sends POST request to reset-password endpoint', () => {
      service.resetPasswordWithCode('user1', '123456', 'newPass').subscribe();
      const req = httpMock.expectOne(`${environment.apiBaseUrl}/auth/reset-password`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ username: 'user1', code: '123456', newPassword: 'newPass' });
      req.flush({});
    });
  });

  describe('2FA flows', () => {
    it('sends POST request to new-secret endpoint', () => {
      service.newTwoFactorSecret().subscribe(res => {
        expect(res).toEqual({ secret: 'sec', otpAuthUri: 'uri' });
      });
      const req = httpMock.expectOne(`${environment.apiBaseUrl}/auth/2fa/new-secret`);
      expect(req.request.method).toBe('POST');
      req.flush({ secret: 'sec', otpAuthUri: 'uri' });
    });

    it('sends POST request to verify endpoint', () => {
      service.verifyTwoFactorCode('sec', '123456').subscribe(res => {
        expect(res.valid).toBeTrue();
      });
      const req = httpMock.expectOne(`${environment.apiBaseUrl}/auth/2fa/verify`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ secret: 'sec', code: '123456' });
      req.flush({ valid: true });
    });
  });

  describe('getRole additional branches', () => {
    it('returns role from fallback role claim key if ROLE_CLAIM_TYPE is not present', () => {
      localStorage.setItem('auth_token', buildToken({ role: 'Manager' }));
      expect(service.getRole()).toBe('Manager');
    });

    it('returns null if role claim is not a string', () => {
      localStorage.setItem('auth_token', buildToken({ role: 123 }));
      expect(service.getRole()).toBeNull();
    });
  });
});
