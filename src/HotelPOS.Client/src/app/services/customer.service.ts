import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Customer, CustomerHistory, SaveCustomerRequest } from '../models/customer.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class CustomerService {
  private readonly apiUrl = `${environment.apiBaseUrl}/customers`;

  constructor(private readonly http: HttpClient) { }

  getCustomers(includeInactive = false): Observable<Customer[]> {
    return this.http.get<Customer[]>(this.apiUrl, { params: { includeInactive } });
  }

  getCustomer(id: number): Observable<Customer> {
    return this.http.get<Customer>(`${this.apiUrl}/${id}`);
  }

  getCustomerHistory(id: number): Observable<CustomerHistory> {
    return this.http.get<CustomerHistory>(`${this.apiUrl}/${id}/history`);
  }

  createCustomer(request: SaveCustomerRequest): Observable<Customer> {
    return this.http.post<Customer>(this.apiUrl, request);
  }

  updateCustomer(id: number, request: SaveCustomerRequest): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, request);
  }

  deleteCustomer(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
