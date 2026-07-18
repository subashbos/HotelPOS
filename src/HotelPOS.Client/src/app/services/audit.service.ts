import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuditLog } from '../models/audit.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuditService {
  private readonly apiUrl = `${environment.apiBaseUrl}/audit`;

  constructor(private readonly http: HttpClient) { }

  getLogs(from?: string, to?: string): Observable<AuditLog[]> {
    const params: Record<string, string> = {};
    if (from) params['from'] = from;
    if (to) params['to'] = to;
    return this.http.get<AuditLog[]>(this.apiUrl, { params });
  }
}
