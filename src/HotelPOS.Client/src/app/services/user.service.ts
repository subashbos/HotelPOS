import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AppUser } from '../models/user.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private readonly apiUrl = `${environment.apiBaseUrl}/users`;

  constructor(private readonly http: HttpClient) { }

  getUsers(): Observable<AppUser[]> {
    return this.http.get<AppUser[]>(this.apiUrl);
  }

  createUser(username: string, password: string, role: string, roleId: number): Observable<void> {
    return this.http.post<void>(this.apiUrl, { username, password, role, roleId });
  }

  toggleActive(id: number, isActive: boolean): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/toggle-active`, { isActive });
  }

  resetPassword(id: number, newPassword: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/reset-password`, { newPassword });
  }

  setTwoFactor(id: number, enabled: boolean, secret: string | null): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/two-factor`, { enabled, secret });
  }

  setEmail(id: number, email: string | null): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/set-email`, { email });
  }

  deleteUser(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
