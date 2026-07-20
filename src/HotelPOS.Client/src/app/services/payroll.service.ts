import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PayrollRun, Payslip, SalaryStructure, SaveSalaryStructureRequest } from '../models/payroll.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PayrollService {
  private readonly apiUrl = `${environment.apiBaseUrl}/payroll`;

  constructor(private readonly http: HttpClient) { }

  getSalaryStructures(employeeId: number): Observable<SalaryStructure[]> {
    return this.http.get<SalaryStructure[]>(`${this.apiUrl}/salary-structures/${employeeId}`);
  }

  saveSalaryStructure(request: SaveSalaryStructureRequest): Observable<SalaryStructure> {
    return this.http.post<SalaryStructure>(`${this.apiUrl}/salary-structures`, request);
  }

  runPayroll(month: number, year: number): Observable<PayrollRun> {
    return this.http.post<PayrollRun>(`${this.apiUrl}/run`, { month, year });
  }

  markRunAsPaid(id: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/runs/${id}/mark-paid`, null);
  }

  getRuns(): Observable<PayrollRun[]> {
    return this.http.get<PayrollRun[]>(`${this.apiUrl}/runs`);
  }

  getRun(id: number): Observable<PayrollRun> {
    return this.http.get<PayrollRun>(`${this.apiUrl}/runs/${id}`);
  }

  getPayslips(employeeId: number): Observable<Payslip[]> {
    return this.http.get<Payslip[]>(`${this.apiUrl}/payslips/${employeeId}`);
  }
}
