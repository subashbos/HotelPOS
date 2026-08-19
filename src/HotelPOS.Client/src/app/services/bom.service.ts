import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { MenuItemBom, BomIngredientRow } from '../models/bom.model';

@Injectable({
  providedIn: 'root'
})
export class BomService {
  private readonly apiUrl = `${environment.apiBaseUrl}/bom`;

  constructor(private readonly http: HttpClient) { }

  getBomForMenuItem(menuItemId: number): Observable<MenuItemBom> {
    return this.http.get<MenuItemBom>(`${this.apiUrl}/menu-item/${menuItemId}`);
  }

  saveBom(menuItemId: number, ingredients: BomIngredientRow[]): Observable<MenuItemBom> {
    return this.http.post<MenuItemBom>(`${this.apiUrl}/menu-item/${menuItemId}`, { ingredients });
  }

  deleteBom(menuItemId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/menu-item/${menuItemId}`);
  }
}
