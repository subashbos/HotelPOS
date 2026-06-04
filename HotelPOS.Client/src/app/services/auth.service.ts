import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

interface LoginResponse {
  token: string;
  username: string;
  role: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = 'https://localhost:55982/api/auth';

  constructor(private http: HttpClient) { }

  login(credentials: { username: string; password: string }): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, credentials).pipe(
      tap(response => {
        if (response?.token) {
          this.saveSession(response.token, response.username, response.role);
        }
      })
    );
  }

  private saveSession(token: string, username: string, role: string): void {
    localStorage.setItem('auth_token', token);
    localStorage.setItem('auth_username', username);
    localStorage.setItem('auth_role', role);
  }

  getToken(): string | null {
    return localStorage.getItem('auth_token');
  }

  getUsername(): string | null {
    return localStorage.getItem('auth_username');
  }

  getRole(): string | null {
    return localStorage.getItem('auth_role');
  }

  isLoggedIn(): boolean {
    return !!this.getToken();
  }

  logout(): void {
    localStorage.removeItem('auth_token');
    localStorage.removeItem('auth_username');
    localStorage.removeItem('auth_role');
  }
}
