import { Component, OnInit } from '@angular/core';
import { ReportService } from '../../../services/report.service';
import { ItemReportRow } from '../../../models/report.model';

function firstOfMonth(): string {
  const d = new Date();
  return new Date(d.getFullYear(), d.getMonth(), 1).toISOString().substring(0, 10);
}
function today(): string {
  return new Date().toISOString().substring(0, 10);
}

@Component({
  standalone: false,
  selector: 'app-item-report',
  templateUrl: './item-report.component.html',
})
export class ItemReportComponent implements OnInit {
  rows: ItemReportRow[] = [];
  isLoading = false;
  loadError = '';

  fromDate = firstOfMonth();
  toDate = today();

  constructor(private readonly reportService: ReportService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.loadError = '';
    this.reportService.getItemReport(this.fromDate, this.toDate).subscribe({
      next: (rows) => {
        this.rows = rows;
        this.isLoading = false;
      },
      error: (err) => {
        this.loadError = 'Failed to load the item report. Please check the server connection.';
        this.isLoading = false;
        console.error('Item report load error:', err);
      }
    });
  }

  get totalRevenue(): number {
    return this.rows.reduce((s, r) => s + r.totalRevenue, 0);
  }
}
