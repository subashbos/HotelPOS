import { Component, OnInit } from '@angular/core';
import { ReportService } from '../../../services/report.service';
import {
  ItemMarginRow, LowStockAlert, MonthlyTrend, ProfitMarginSummary, WastageSummary
} from '../../../models/report.model';

function firstOfMonth(): string {
  const d = new Date();
  return new Date(d.getFullYear(), d.getMonth(), 1).toISOString().substring(0, 10);
}
function today(): string {
  return new Date().toISOString().substring(0, 10);
}

@Component({
  standalone: false,
  selector: 'app-journal',
  templateUrl: './journal.component.html',
})
export class JournalComponent implements OnInit {
  summary: ProfitMarginSummary | null = null;
  itemMargins: ItemMarginRow[] = [];
  wastage: WastageSummary | null = null;
  lowStock: LowStockAlert[] = [];
  monthlyTrend: MonthlyTrend[] = [];

  isLoading = false;
  loadError = '';

  fromDate = firstOfMonth();
  toDate = today();

  constructor(private readonly reportService: ReportService) {}

  ngOnInit(): void {
    this.load();
    this.reportService.getLowStockAlerts().subscribe({
      next: (alerts) => (this.lowStock = alerts),
      error: (err) => console.error('Low stock alerts load error:', err)
    });
    this.reportService.getMonthlyTrend().subscribe({
      next: (trend) => (this.monthlyTrend = trend),
      error: (err) => console.error('Monthly trend load error:', err)
    });
  }

  load(): void {
    this.isLoading = true;
    this.loadError = '';
    this.reportService.getMarginSummary(this.fromDate, this.toDate).subscribe({
      next: (summary) => {
        this.summary = summary;
        this.isLoading = false;
      },
      error: (err) => {
        this.loadError = 'Failed to load the journal. Please check the server connection.';
        this.isLoading = false;
        console.error('Margin summary load error:', err);
      }
    });
    this.reportService.getItemMargins(this.fromDate, this.toDate).subscribe({
      next: (rows) => (this.itemMargins = rows),
      error: (err) => console.error('Item margins load error:', err)
    });
    this.reportService.getWastageSummary(this.fromDate, this.toDate).subscribe({
      next: (summary) => (this.wastage = summary),
      error: (err) => console.error('Wastage summary load error:', err)
    });
  }
}
