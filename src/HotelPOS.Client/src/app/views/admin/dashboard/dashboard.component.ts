import { Component, OnInit } from '@angular/core';
import { ReportService } from '../../../services/report.service';
import { LowStockAlert, MonthlySalesChart, SalesReport } from '../../../models/report.model';

function firstOfMonth(): string {
  const d = new Date();
  return new Date(d.getFullYear(), d.getMonth(), 1).toISOString().substring(0, 10);
}
function today(): string {
  return new Date().toISOString().substring(0, 10);
}

@Component({
  standalone: false,
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent implements OnInit {
  report: SalesReport | null = null;
  monthlyChart: MonthlySalesChart[] = [];
  lowStock: LowStockAlert[] = [];

  isLoading = false;
  loadError = '';

  constructor(private readonly reportService: ReportService) {}

  ngOnInit(): void {
    this.isLoading = true;
    this.loadError = '';
    this.reportService.getSalesReport(firstOfMonth(), today()).subscribe({
      next: (report) => {
        this.report = report;
        this.isLoading = false;
      },
      error: (err) => {
        this.loadError = 'Failed to load dashboard data. Please check the server connection.';
        this.isLoading = false;
        console.error('Dashboard sales report load error:', err);
      }
    });
    this.reportService.getMonthlyChart().subscribe({
      next: (chart) => (this.monthlyChart = chart),
      error: (err) => console.error('Dashboard monthly chart load error:', err)
    });
    this.reportService.getLowStockAlerts().subscribe({
      next: (alerts) => (this.lowStock = alerts.filter((a) => a.alertLevel !== 'Normal')),
      error: (err) => console.error('Dashboard low stock load error:', err)
    });
  }

  get maxMonthlyRevenue(): number {
    return Math.max(1, ...this.monthlyChart.map((m) => m.revenue));
  }
}
