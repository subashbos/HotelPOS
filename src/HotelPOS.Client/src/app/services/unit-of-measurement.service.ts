import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { UnitOfMeasurement } from '../models/item.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class UnitOfMeasurementService {
  private readonly apiUrl = `${environment.apiBaseUrl}/unitofmeasurements`;

  constructor(private readonly http: HttpClient) { }

  getUnits(): Observable<UnitOfMeasurement[]> {
    return this.http.get<UnitOfMeasurement[]>(this.apiUrl);
  }

  createUnit(unit: Partial<UnitOfMeasurement>): Observable<UnitOfMeasurement> {
    return this.http.post<UnitOfMeasurement>(this.apiUrl, unit);
  }

  updateUnit(id: number, unit: Partial<UnitOfMeasurement>): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, unit);
  }

  deleteUnit(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
