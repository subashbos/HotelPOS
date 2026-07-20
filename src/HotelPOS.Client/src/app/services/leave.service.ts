import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApplyLeaveRequest, LeaveBalance, LeaveRequest, LeaveType } from '../models/leave.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class LeaveService {
  private readonly apiUrl = `${environment.apiBaseUrl}/leave`;

  constructor(private readonly http: HttpClient) { }

  getLeaveTypes(): Observable<LeaveType[]> {
    return this.http.get<LeaveType[]>(`${this.apiUrl}/types`);
  }

  getBalances(employeeId: number, year?: number): Observable<LeaveBalance[]> {
    const params = year ? { year } : {};
    return this.http.get<LeaveBalance[]>(`${this.apiUrl}/balances/${employeeId}`, { params });
  }

  getRequests(employeeId?: number, status?: string): Observable<LeaveRequest[]> {
    const params: Record<string, string | number> = {};
    if (employeeId) params['employeeId'] = employeeId;
    if (status) params['status'] = status;
    return this.http.get<LeaveRequest[]>(`${this.apiUrl}/requests`, { params });
  }

  applyLeave(request: ApplyLeaveRequest): Observable<LeaveRequest> {
    return this.http.post<LeaveRequest>(`${this.apiUrl}/requests`, request);
  }

  approveLeave(id: number, approverEmployeeId: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/requests/${id}/approve`, null, { params: { approverEmployeeId } });
  }

  rejectLeave(id: number, approverEmployeeId: number, reason: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/requests/${id}/reject`, { reason }, { params: { approverEmployeeId } });
  }
}
