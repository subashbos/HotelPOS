import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, of, throwError } from 'rxjs';
import { CashSession } from '../models/cash-session.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class CashSessionService {
  private readonly apiUrl = `${environment.apiBaseUrl}/cashsessions`;

  constructor(private readonly http: HttpClient) { }

  getCurrentSession(): Observable<CashSession | null> {
    return this.http.get<CashSession>(`${this.apiUrl}/current`).pipe(
      catchError((err) => (err.status === 404 ? of(null) : throwError(() => err)))
    );
  }

  getHistory(count = 30): Observable<CashSession[]> {
    return this.http.get<CashSession[]>(`${this.apiUrl}/history`, { params: { count } });
  }

  getCurrentSessionSalesTotal(): Observable<number> {
    return this.http.get<number>(`${this.apiUrl}/current/sales-total`);
  }

  openSession(openingBalance: number): Observable<number> {
    return this.http.post<number>(`${this.apiUrl}/open`, { openingBalance });
  }

  closeSession(actualCash: number, notes?: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/close`, { actualCash, notes });
  }
}
