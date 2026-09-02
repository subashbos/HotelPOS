import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { RawMaterial } from '../models/raw-material.model';

@Injectable({
  providedIn: 'root'
})
export class RawMaterialService {
  private readonly apiUrl = `${environment.apiBaseUrl}/rawmaterials`;

  constructor(private readonly http: HttpClient) { }

  getRawMaterials(): Observable<RawMaterial[]> {
    return this.http.get<RawMaterial[]>(this.apiUrl);
  }

  createRawMaterial(material: Partial<RawMaterial>): Observable<RawMaterial> {
    return this.http.post<RawMaterial>(this.apiUrl, material);
  }

  updateRawMaterial(id: number, material: Partial<RawMaterial>): Observable<RawMaterial> {
    return this.http.put<RawMaterial>(`${this.apiUrl}/${id}`, material);
  }

  deleteRawMaterial(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
