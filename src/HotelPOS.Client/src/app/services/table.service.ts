import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DiningTable } from '../models/table.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class TableService {
  private readonly apiUrl = `${environment.apiBaseUrl}/tables`;

  constructor(private readonly http: HttpClient) { }

  getTables(): Observable<DiningTable[]> {
    return this.http.get<DiningTable[]>(this.apiUrl);
  }

  createTable(table: Partial<DiningTable>): Observable<DiningTable> {
    return this.http.post<DiningTable>(this.apiUrl, table);
  }

  updateTable(id: number, table: Partial<DiningTable>): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, table);
  }

  deleteTable(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
