import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Purchase, SavePurchaseRequest } from '../models/purchase.model';
import { Supplier } from '../models/supplier.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PurchaseService {
  private readonly apiUrl = `${environment.apiBaseUrl}/purchases`;

  constructor(private readonly http: HttpClient) { }

  getPurchases(): Observable<Purchase[]> {
    return this.http.get<Purchase[]>(this.apiUrl);
  }

  getSuppliers(): Observable<Supplier[]> {
    return this.http.get<Supplier[]>(`${this.apiUrl}/suppliers`);
  }

  createPurchase(request: SavePurchaseRequest): Observable<Purchase> {
    return this.http.post<Purchase>(this.apiUrl, request);
  }
}
