import { Component, OnInit } from '@angular/core';
import { ReportService } from '../../../services/report.service';
import { PagedPurchaseReport } from '../../../models/report.model';

@Component({
  standalone: false,
  selector: 'app-purchase-report',
  templateUrl: './purchase-report.component.html',
})
export class PurchaseReportComponent implements OnInit {
  report: PagedPurchaseReport | null = null;
  isLoading = false;
  loadError = '';

  page = 1;
  pageSize = 20;
  fromDate = '';
  toDate = '';

  constructor(private readonly reportService: ReportService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.loadError = '';
    this.reportService.getPurchaseReport(this.page, this.pageSize, this.fromDate || undefined, this.toDate || undefined).subscribe({
      next: (report) => {
        this.report = report;
        this.isLoading = false;
      },
      error: (err) => {
        this.loadError = 'Failed to load the purchase report. Please check the server connection.';
        this.isLoading = false;
        console.error('Purchase report load error:', err);
      }
    });
  }

  get totalPages(): number {
    if (!this.report) return 1;
    return Math.max(1, Math.ceil(this.report.totalCount / this.pageSize));
  }

  prevPage(): void {
    if (this.page > 1) {
      this.page -= 1;
      this.load();
    }
  }

  nextPage(): void {
    if (this.page < this.totalPages) {
      this.page += 1;
      this.load();
    }
  }
}
