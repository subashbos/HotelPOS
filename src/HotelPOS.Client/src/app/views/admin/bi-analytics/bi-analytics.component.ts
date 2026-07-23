import { Component, OnInit } from '@angular/core';
import { BiAnalyticsService, BiOverviewKpis, MonthlyTrendBar } from '../../../services/bi-analytics.service';
import { ReportService } from '../../../services/report.service';
import {
  ProfitAndLossReport, ShiftClosureReport, StaffPerformanceReport,
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
  activeTab: 'overview' | 'shift' | 'voids' | 'staff' | 'valuation' | 'pnl' = 'overview';

  kpis: BiOverviewKpis = {
    totalRevenue: 485000,
    netProfit: 142000,
    foodCostPercentage: 31.5,
    totalWastageCost: 12400,
    cogs: 152775,
    totalExpenses: 190225
  };

  monthlyTrends: MonthlyTrendBar[] = [
    { monthName: 'Jan', revenue: 380000, profit: 98000 },
    { monthName: 'Feb', revenue: 410000, profit: 112000 },
    { monthName: 'Mar', revenue: 450000, profit: 128000 },
    { monthName: 'Apr', revenue: 420000, profit: 115000 },
    { monthName: 'May', revenue: 490000, profit: 145000 },
    { monthName: 'Jun', revenue: 485000, profit: 142000 }
  ];

  topMarginItems = [
    { name: 'Cold Coffee', category: 'Beverages', price: 120, foodCost: 22, marginPct: 81.6 },
    { name: 'Veg Fried Rice', category: 'Main Course', price: 180, foodCost: 45, marginPct: 75.0 },
    { name: 'Paneer Butter Masala', category: 'Main Course', price: 280, foodCost: 90.6, marginPct: 67.6 },
    { name: 'Butter Chicken', category: 'Main Course', price: 340, foodCost: 97.25, marginPct: 71.4 }
  ];

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

    this.biAnalyticsService.getBiAnalytics(this.fromDate, this.toDate).subscribe({
      next: (res) => {
        if (res && res.kpis) {
          this.kpis = res.kpis;
          this.monthlyTrends = res.monthlyTrends || this.monthlyTrends;
        }
      },
      error: () => {}
    });

    // Load new reports
    this.reportService.getShiftClosureReport(undefined, this.fromDate).subscribe(data => this.shiftReport = data);
    this.reportService.getVoidDiscountAudit(this.fromDate, this.toDate).subscribe(data => this.voidAudits = data);
    this.reportService.getStaffPerformanceReport(this.fromDate, this.toDate).subscribe(data => this.staffReports = data);
    this.reportService.getStockValuationReport().subscribe(data => this.stockValuation = data);
    this.reportService.getProfitAndLossReport(this.fromDate, this.toDate).subscribe(data => {
      this.pnlReport = data;
      this.isLoading = false;
    }, () => this.isLoading = false);
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
