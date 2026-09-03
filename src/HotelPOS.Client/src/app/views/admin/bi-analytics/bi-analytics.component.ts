import { Component, OnInit } from '@angular/core';
import { BiAnalyticsService, BiOverviewKpis, MonthlyTrendBar } from '../../../services/bi-analytics.service';
import { ReportService } from '../../../services/report.service';
import {
  ItemMarginRow, ProfitAndLossReport, ShiftClosureReport, StaffPerformanceReport,
  StockValuationSummary, VoidDiscountAuditRow
} from '../../../models/report.model';

@Component({
  standalone: false,
  selector: 'app-bi-analytics',
  templateUrl: './bi-analytics.component.html'
})
export class BiAnalyticsComponent implements OnInit {
  fromDate = '';
  toDate = '';
  isLoading = false;
  errorMessage = '';
  activeTab: 'overview' | 'shift' | 'voids' | 'staff' | 'valuation' | 'pnl' = 'overview';

  kpis: BiOverviewKpis = {
    totalRevenue: 0,
    netProfit: 0,
    foodCostPercentage: 0,
    totalWastageCost: 0,
    cogs: 0,
    totalExpenses: 0
  };

  monthlyTrends: MonthlyTrendBar[] = [];

  topMarginItems: ItemMarginRow[] = [];

  // Report States
  shiftReport: ShiftClosureReport | null = null;
  voidAudits: VoidDiscountAuditRow[] = [];
  staffReports: StaffPerformanceReport[] = [];
  stockValuation: StockValuationSummary | null = null;
  pnlReport: ProfitAndLossReport | null = null;

  constructor(
    private readonly biAnalyticsService: BiAnalyticsService,
    private readonly reportService: ReportService
  ) {}

  ngOnInit(): void {
    const now = new Date();
    const firstDay = new Date(now.getFullYear(), now.getMonth(), 1);
    this.fromDate = firstDay.toISOString().split('T')[0];
    this.toDate = now.toISOString().split('T')[0];
    this.loadData();
  }

  setTab(tab: 'overview' | 'shift' | 'voids' | 'staff' | 'valuation' | 'pnl'): void {
    this.activeTab = tab;
  }

  loadData(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.biAnalyticsService.getBiAnalytics(this.fromDate, this.toDate).subscribe({
      next: (res) => {
        if (res?.kpis) {
          this.kpis = res.kpis;
          this.monthlyTrends = res.monthlyTrends || [];
        }
      },
      error: (err) => {
        this.errorMessage = this.errorMessage || 'Failed to load the overview KPIs. Please check the server connection.';
        console.error('BI overview load error:', err);
      }
    });

    this.reportService.getItemMargins(this.fromDate, this.toDate).subscribe({
      next: (rows) => {
        this.topMarginItems = [...rows].sort((a, b) => b.marginPercentage - a.marginPercentage).slice(0, 5);
      },
      error: (err) => {
        this.topMarginItems = [];
        console.error('Item margins load error:', err);
      }
    });

    // Load new reports
    this.reportService.getShiftClosureReport(undefined, this.fromDate).subscribe({
      next: (data) => this.shiftReport = data,
      error: (err) => console.error('Shift closure report load error:', err)
    });
    this.reportService.getVoidDiscountAudit(this.fromDate, this.toDate).subscribe({
      next: (data) => this.voidAudits = data,
      error: (err) => console.error('Void/discount audit load error:', err)
    });
    this.reportService.getStaffPerformanceReport(this.fromDate, this.toDate).subscribe({
      next: (data) => this.staffReports = data,
      error: (err) => console.error('Staff performance report load error:', err)
    });
    this.reportService.getStockValuationReport().subscribe({
      next: (data) => this.stockValuation = data,
      error: (err) => console.error('Stock valuation report load error:', err)
    });
    this.reportService.getProfitAndLossReport(this.fromDate, this.toDate).subscribe({
      next: (data) => {
        this.pnlReport = data;
        this.isLoading = false;
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = this.errorMessage || 'Failed to load the profit & loss report. Please check the server connection.';
        console.error('P&L report load error:', err);
      }
    });
  }

  refresh(): void {
    this.loadData();
  }

  getMaxRevenue(): number {
    return Math.max(...this.monthlyTrends.map(t => t.revenue), 500000);
  }

  getBarHeightPct(val: number): number {
    const max = this.getMaxRevenue();
    return Math.max(10, Math.min(100, (val / max) * 100));
  }
}
