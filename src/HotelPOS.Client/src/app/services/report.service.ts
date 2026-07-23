import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  GstReportRow, ItemMarginRow, ItemReportRow, LowStockAlert, MonthlySalesChart, MonthlyTrend,
  PagedPurchaseReport, ProfitAndLossReport, ProfitMarginSummary, SalesReport, ShiftClosureReport, StaffPerformanceReport, StockValuationSummary, VoidDiscountAuditRow, WastageSummary
} from '../models/report.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ReportService {
  private readonly apiUrl = `${environment.apiBaseUrl}/reports`;

  constructor(private readonly http: HttpClient) { }

  getSalesReport(from?: string, to?: string): Observable<SalesReport> {
    return this.http.get<SalesReport>(`${this.apiUrl}/sales`, { params: this.dateParams(from, to) });
  }

  getItemReport(from?: string, to?: string): Observable<ItemReportRow[]> {
    return this.http.get<ItemReportRow[]>(`${this.apiUrl}/items`, { params: this.dateParams(from, to) });
  }

  getGstReport(from: string, to: string): Observable<GstReportRow[]> {
    return this.http.get<GstReportRow[]>(`${this.apiUrl}/gst`, { params: { from, to } });
  }

  getMonthlyChart(): Observable<MonthlySalesChart[]> {
    return this.http.get<MonthlySalesChart[]>(`${this.apiUrl}/monthly-chart`);
  }

  getPurchaseReport(page: number, pageSize: number, from?: string, to?: string): Observable<PagedPurchaseReport> {
    return this.http.get<PagedPurchaseReport>(`${this.apiUrl}/purchases`, {
      params: { page, pageSize, ...this.dateParams(from, to) }
    });
  }

  getMarginSummary(from?: string, to?: string): Observable<ProfitMarginSummary> {
    return this.http.get<ProfitMarginSummary>(`${this.apiUrl}/margins/summary`, { params: this.dateParams(from, to) });
  }

  getItemMargins(from?: string, to?: string): Observable<ItemMarginRow[]> {
    return this.http.get<ItemMarginRow[]>(`${this.apiUrl}/margins/items`, { params: this.dateParams(from, to) });
  }

  getWastageSummary(from?: string, to?: string): Observable<WastageSummary> {
    return this.http.get<WastageSummary>(`${this.apiUrl}/wastage`, { params: this.dateParams(from, to) });
  }

  getLowStockAlerts(): Observable<LowStockAlert[]> {
    return this.http.get<LowStockAlert[]>(`${this.apiUrl}/low-stock`);
  }

  getMonthlyTrend(): Observable<MonthlyTrend[]> {
    return this.http.get<MonthlyTrend[]>(`${this.apiUrl}/monthly-trend`);
  }

  getShiftClosureReport(sessionId?: number, date?: string): Observable<ShiftClosureReport> {
    const params: Record<string, string> = {};
    if (sessionId) params['sessionId'] = sessionId.toString();
    if (date) params['date'] = date;
    return this.http.get<ShiftClosureReport>(`${this.apiUrl}/shift-closure`, { params });
  }

  getVoidDiscountAudit(from?: string, to?: string): Observable<VoidDiscountAuditRow[]> {
    return this.http.get<VoidDiscountAuditRow[]>(`${this.apiUrl}/void-audit`, { params: this.dateParams(from, to) });
  }

  getStaffPerformanceReport(from?: string, to?: string): Observable<StaffPerformanceReport[]> {
    return this.http.get<StaffPerformanceReport[]>(`${this.apiUrl}/staff-performance`, { params: this.dateParams(from, to) });
  }

  getStockValuationReport(): Observable<StockValuationSummary> {
    return this.http.get<StockValuationSummary>(`${this.apiUrl}/stock-valuation`);
  }

  getProfitAndLossReport(from?: string, to?: string): Observable<ProfitAndLossReport> {
    return this.http.get<ProfitAndLossReport>(`${this.apiUrl}/pnl`, { params: this.dateParams(from, to) });
  }

  private dateParams(from?: string, to?: string): Record<string, string> {
    const params: Record<string, string> = {};
    if (from) params['from'] = from;
    if (to) params['to'] = to;
    return params;
  }
}
