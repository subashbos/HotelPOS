import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Role, RolePermission } from '../models/role.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class RoleService {
  private readonly apiUrl = `${environment.apiBaseUrl}/roles`;

  constructor(private readonly http: HttpClient) { }

  getRoles(): Observable<Role[]> {
    return this.http.get<Role[]>(this.apiUrl);
  }

  getRole(id: number): Observable<Role> {
    return this.http.get<Role>(`${this.apiUrl}/${id}`);
  }

  createRole(name: string, description: string): Observable<void> {
    return this.http.post<void>(this.apiUrl, { name, description });
  }

  updatePermissions(roleId: number, permissions: RolePermission[]): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${roleId}/permissions`, permissions);
  }

  deleteRole(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
