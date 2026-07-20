import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';

interface LoginResponse {
  token: string;
  refreshToken: string;
  username: string;
  role: string;
}

// ASP.NET Core writes System.Security.Claims.ClaimTypes.Role claims using this full URI
// as the JWT payload key (it is not remapped to the short "role" name for tokens built
// directly from a Claim[], as AuthController does).
const ROLE_CLAIM_TYPE = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = `${environment.apiBaseUrl}/auth`;

  // Refresh tokens are kept in memory only, never localStorage: they are longer-lived
  // than the access token, so persisting them would hand an XSS payload a durable way
  // to mint fresh access tokens even after the tab is closed and reopened.
  private refreshTokenValue: string | null = null;

  constructor(private readonly http: HttpClient) { }

  login(credentials: { username: string; password: string; totpCode?: string }): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, credentials).pipe(
      tap(response => {
        if (response?.token) {
          this.saveSession(response.token, response.username, response.refreshToken);
        }
      })
    );
  }

  /** Exchanges the in-memory refresh token for a new access/refresh token pair. */
  refresh(): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/refresh`, { refreshToken: this.refreshTokenValue }).pipe(
      tap(response => {
        if (response?.token) {
          this.saveSession(response.token, response.username, response.refreshToken);
        }
      })
    );
  }

  hasRefreshToken(): boolean {
    return !!this.refreshTokenValue;
  }

  private saveSession(token: string, username: string, refreshToken: string): void {
    localStorage.setItem('auth_token', token);
    localStorage.setItem('auth_username', username);
    this.refreshTokenValue = refreshToken;
  }

  getToken(): string | null {
    return localStorage.getItem('auth_token');
  }

  getUsername(): string | null {
    return localStorage.getItem('auth_username');
  }

  /**
   * Role is read from the current JWT's claims rather than a separate localStorage key,
   * so it can't be spoofed by editing devtools storage without forging a signed token.
   */
  getRole(): string | null {
    const token = this.getToken();
    if (!token) return null;

    const payload = this.decodeToken(token);
    const role = payload?.[ROLE_CLAIM_TYPE] ?? payload?.['role'];
    return typeof role === 'string' ? role : null;
  }

  isLoggedIn(): boolean {
    return !!this.getToken();
  }

  /** User ID is read from the JWT's "sub" claim, set once at login and never persisted separately. */
  getUserId(): number | null {
    const token = this.getToken();
    if (!token) return null;

    const payload = this.decodeToken(token);
    const sub = payload?.['sub'];
    const id = typeof sub === 'string' ? parseInt(sub, 10) : null;
    return id !== null && !isNaN(id) ? id : null;
  }

  forgotPassword(username: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/forgot-password`, { username });
  }

  resetPasswordWithCode(username: string, code: string, newPassword: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/reset-password`, { username, code, newPassword });
  }

  newTwoFactorSecret(): Observable<{ secret: string; otpAuthUri: string }> {
    return this.http.post<{ secret: string; otpAuthUri: string }>(`${this.apiUrl}/2fa/new-secret`, {});
  }

  verifyTwoFactorCode(secret: string, code: string): Observable<{ valid: boolean }> {
    return this.http.post<{ valid: boolean }>(`${this.apiUrl}/2fa/verify`, { secret, code });
  }

  logout(): void {
    const refreshToken = this.refreshTokenValue;

    localStorage.removeItem('auth_token');
    localStorage.removeItem('auth_username');
    this.refreshTokenValue = null;

    if (refreshToken) {
      // Best-effort server-side revocation; the client-side session is already cleared either way.
      this.http.post(`${this.apiUrl}/logout`, { refreshToken }).subscribe({ error: () => { /* ignore */ } });
    }
  }

  private decodeToken(token: string): Record<string, unknown> | null {
    try {
      const payload = token.split('.')[1];
      const normalized = payload.replaceAll('-', '+').replaceAll('_', '/');
      return JSON.parse(atob(normalized));
    } catch {
      return null;
    }
  }
}
