import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Employee, EssProfileUpdateRequest } from '../models/employee.model';
import { ApplyLeaveRequest, LeaveBalance, LeaveRequest, LeaveType } from '../models/leave.model';
import { Payslip } from '../models/payroll.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class EssService {
  private readonly apiUrl = `${environment.apiBaseUrl}/ess`;

  constructor(private readonly http: HttpClient) { }

  getProfile(): Observable<Employee> {
    return this.http.get<Employee>(`${this.apiUrl}/profile`);
  }

  updateProfile(request: EssProfileUpdateRequest): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/profile`, request);
  }

  getLeaveTypes(): Observable<LeaveType[]> {
    return this.http.get<LeaveType[]>(`${this.apiUrl}/leave/types`);
  }

  getLeaveBalances(year?: number): Observable<LeaveBalance[]> {
    const params = year ? { year } : {};
    return this.http.get<LeaveBalance[]>(`${this.apiUrl}/leave/balances`, { params });
  }

  getLeaveRequests(status?: string): Observable<LeaveRequest[]> {
    const params = status ? { status } : {};
    return this.http.get<LeaveRequest[]>(`${this.apiUrl}/leave/requests`, { params });
  }

  // employeeId is resolved server-side from the caller's own login and ignored if sent, so the
  // self-service request type omits it entirely rather than accepting a value nobody should set.
  applyLeave(request: Omit<ApplyLeaveRequest, 'employeeId'>): Observable<LeaveRequest> {
    return this.http.post<LeaveRequest>(`${this.apiUrl}/leave/requests`, request);
  }

  cancelLeave(id: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/leave/requests/${id}/cancel`, null);
  }

  getPayslips(): Observable<Payslip[]> {
    return this.http.get<Payslip[]>(`${this.apiUrl}/payslips`);
  }
}
