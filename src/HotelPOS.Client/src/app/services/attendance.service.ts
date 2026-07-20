import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Attendance, MarkAttendanceRequest } from '../models/attendance.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AttendanceService {
  private readonly apiUrl = `${environment.apiBaseUrl}/attendance`;

  constructor(private readonly http: HttpClient) { }

  getAttendance(employeeId: number, from: string, to: string): Observable<Attendance[]> {
    return this.http.get<Attendance[]>(this.apiUrl, { params: { employeeId, from, to } });
  }

  markAttendance(request: MarkAttendanceRequest): Observable<Attendance> {
    return this.http.post<Attendance>(this.apiUrl, request);
  }

  deleteAttendance(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
