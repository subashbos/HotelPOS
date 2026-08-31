import { Component, OnInit } from '@angular/core';
import { ReportService } from '../../../services/report.service';
import { SalesReport } from '../../../models/report.model';
import { downloadBlob } from '../../../utils/download.util';

function firstOfMonth(): string {
  const d = new Date();
  return new Date(d.getFullYear(), d.getMonth(), 1).toISOString().substring(0, 10);
}
function today(): string {
  return new Date().toISOString().substring(0, 10);
}

@Component({
  standalone: false,
  selector: 'app-sales-report',
  templateUrl: './sales-report.component.html',
})
export class SalesReportComponent implements OnInit {
  report: SalesReport | null = null;
  isLoading = false;
  loadError = '';
  isExporting = false;

  fromDate = firstOfMonth();
  toDate = today();

  constructor(private readonly reportService: ReportService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.loadError = '';
    this.reportService.getSalesReport(this.fromDate, this.toDate).subscribe({
      next: (report) => {
        this.report = report;
        this.isLoading = false;
      },
      error: (err) => {
        this.loadError = 'Failed to load the sales report. Please check the server connection.';
        this.isLoading = false;
        console.error('Sales report load error:', err);
      }
    });
  }

  export(): void {
    this.isExporting = true;
    this.reportService.exportSalesReport(this.fromDate, this.toDate).subscribe({
      next: (blob) => {
        downloadBlob(blob, `Sales_Report_${this.fromDate}_to_${this.toDate}.xlsx`);
        this.isExporting = false;
      },
      error: (err) => {
        this.isExporting = false;
        console.error('Sales report export error:', err);
      }
    });
  }
}
