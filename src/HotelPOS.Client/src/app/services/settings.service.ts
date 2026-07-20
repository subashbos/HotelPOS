import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SaveSettingsRequest, SystemSettings } from '../models/settings.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class SettingsService {
  private readonly apiUrl = `${environment.apiBaseUrl}/settings`;

  constructor(private readonly http: HttpClient) { }

  getSettings(): Observable<SystemSettings> {
    return this.http.get<SystemSettings>(this.apiUrl);
  }

  saveSettings(request: SaveSettingsRequest): Observable<void> {
    return this.http.put<void>(this.apiUrl, request);
  }
}
