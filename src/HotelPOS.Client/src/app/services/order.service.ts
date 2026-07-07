import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreateOrderRequest } from '../models/order.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class OrderService {
  private readonly apiUrl = `${environment.apiBaseUrl}/orders`;

  constructor(private readonly http: HttpClient) { }

  createOrder(request: CreateOrderRequest): Observable<number> {
    return this.http.post<number>(this.apiUrl, request);
  }
}
