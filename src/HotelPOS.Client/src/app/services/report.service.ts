import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  GstR1Report, ItemMarginRow, ItemReportRow, LedgerReportRow, LowStockAlert, MonthlySalesChart, MonthlyTrend,
  PagedPurchaseReport, ProfitAndLossReport, ProfitMarginSummary, PurchaseReportFilters, SalesReport, ShiftClosureReport,
  StaffPerformanceReport, StockValuationSummary, VoidDiscountAuditRow, WastageSummary
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

  exportSalesReport(from?: string, to?: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/sales/export`, { params: this.dateParams(from, to), responseType: 'blob' });
  }

  getItemReport(from?: string, to?: string): Observable<ItemReportRow[]> {
    return this.http.get<ItemReportRow[]>(`${this.apiUrl}/items`, { params: this.dateParams(from, to) });
  }

  exportItemReport(from?: string, to?: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/items/export`, { params: this.dateParams(from, to), responseType: 'blob' });
  }

  getLedgerReport(from: string, to: string): Observable<LedgerReportRow[]> {
    return this.http.get<LedgerReportRow[]>(`${this.apiUrl}/ledger`, { params: { from, to } });
  }

  exportLedgerReport(from: string, to: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/ledger/export`, { params: { from, to }, responseType: 'blob' });
  }

  getGstR1Report(from: string, to: string): Observable<GstR1Report> {
    return this.http.get<GstR1Report>(`${this.apiUrl}/gstr1`, { params: { from, to } });
  }

  exportGstR1Report(from: string, to: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/gstr1/export`, { params: { from, to }, responseType: 'blob' });
  }

  getMonthlyChart(): Observable<MonthlySalesChart[]> {
    return this.http.get<MonthlySalesChart[]>(`${this.apiUrl}/monthly-chart`);
  }

  getPurchaseReport(page: number, pageSize: number, from?: string, to?: string, filters?: PurchaseReportFilters): Observable<PagedPurchaseReport> {
    return this.http.get<PagedPurchaseReport>(`${this.apiUrl}/purchases`, {
      params: { page, pageSize, ...this.dateParams(from, to), ...this.purchaseFilterParams(filters) }
    });
  }

  exportPurchaseReport(from?: string, to?: string, filters?: PurchaseReportFilters): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/purchases/export`, {
      params: { ...this.dateParams(from, to), ...this.purchaseFilterParams(filters) },
      responseType: 'blob'
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

  private purchaseFilterParams(filters?: PurchaseReportFilters): Record<string, string> {
    const params: Record<string, string> = {};
    if (filters?.supplierId) params['supplierId'] = filters.supplierId.toString();
    if (filters?.itemName) params['itemName'] = filters.itemName;
    if (filters?.paymentType) params['paymentType'] = filters.paymentType;
    if (filters?.invoiceNo) params['invoiceNo'] = filters.invoiceNo;
    return params;
  }
}
