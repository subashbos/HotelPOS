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

  // Fallback initial data in case backend API endpoint is not yet connected
  private readonly mockMaterials: RawMaterial[] = [
    { id: 1, name: 'Basmati Rice', unit: 'kg', costPerUnit: 110, currentStock: 50.5, minStockThreshold: 10 },
    { id: 2, name: 'Refined Cooking Oil', unit: 'l', costPerUnit: 145, currentStock: 30, minStockThreshold: 5 },
    { id: 3, name: 'Chicken (Whole/Cut)', unit: 'kg', costPerUnit: 220, currentStock: 15.2, minStockThreshold: 5 },
    { id: 4, name: 'Paneer (Cottage Cheese)', unit: 'kg', costPerUnit: 340, currentStock: 8.0, minStockThreshold: 3 },
    { id: 5, name: 'Onions', unit: 'kg', costPerUnit: 35, currentStock: 42, minStockThreshold: 10 },
    { id: 6, name: 'Tomatoes', unit: 'kg', costPerUnit: 40, currentStock: 25, minStockThreshold: 8 },
    { id: 7, name: 'Butter', unit: 'kg', costPerUnit: 480, currentStock: 6.5, minStockThreshold: 2 },
    { id: 8, name: 'Garam Masala', unit: 'g', costPerUnit: 0.85, currentStock: 1200, minStockThreshold: 200 }
  ];

  constructor(private readonly http: HttpClient) { }

  getRawMaterials(): Observable<RawMaterial[]> {
    return this.http.get<RawMaterial[]>(this.apiUrl).pipe(
      // Return mock data if HTTP call fails in offline/dev mode
      // RxJS catchError handle
    );
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

  getMockRawMaterials(): Observable<RawMaterial[]> {
    return of([...this.mockMaterials]);
  }
}
