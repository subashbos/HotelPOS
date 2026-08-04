import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ConvertEstimationResult, Estimation, SaveEstimationRequest } from '../models/estimation.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class EstimationService {
  private readonly apiUrl = `${environment.apiBaseUrl}/estimations`;

  constructor(private readonly http: HttpClient) { }

  getEstimations(): Observable<Estimation[]> {
    return this.http.get<Estimation[]>(this.apiUrl);
  }

  createEstimation(request: SaveEstimationRequest): Observable<Estimation> {
    return this.http.post<Estimation>(this.apiUrl, request);
  }

  updateEstimation(id: number, request: SaveEstimationRequest): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, request);
  }

  deleteEstimation(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  convertToOrder(id: number): Observable<ConvertEstimationResult> {
    return this.http.post<ConvertEstimationResult>(`${this.apiUrl}/${id}/convert-to-order`, {});
  }
}
