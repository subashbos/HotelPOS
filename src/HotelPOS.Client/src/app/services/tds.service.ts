import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SaveTdsRuleSetRequest, TdsRuleSet } from '../models/tds.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class TdsService {
  private readonly apiUrl = `${environment.apiBaseUrl}/tds`;

  constructor(private readonly http: HttpClient) { }

  getFinancialYears(): Observable<number[]> {
    return this.http.get<number[]>(`${this.apiUrl}/financial-years`);
  }

  getRuleSet(financialYearStart: number): Observable<TdsRuleSet> {
    return this.http.get<TdsRuleSet>(`${this.apiUrl}/rule-sets/${financialYearStart}`);
  }

  saveRuleSet(financialYearStart: number, request: SaveTdsRuleSetRequest): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/rule-sets/${financialYearStart}`, request);
  }

  deleteRuleSet(financialYearStart: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/rule-sets/${financialYearStart}`);
  }
}
