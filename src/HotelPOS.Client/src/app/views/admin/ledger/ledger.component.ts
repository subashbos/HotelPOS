import { Component, OnInit } from '@angular/core';
import { ReportService } from '../../../services/report.service';
import { GstReportRow } from '../../../models/report.model';

function firstOfMonth(): string {
  const d = new Date();
  return new Date(d.getFullYear(), d.getMonth(), 1).toISOString().substring(0, 10);
}
function today(): string {
  return new Date().toISOString().substring(0, 10);
}

@Component({
  standalone: false,
  selector: 'app-ledger',
  templateUrl: './ledger.component.html',
})
export class LedgerComponent implements OnInit {
  rows: GstReportRow[] = [];
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
    this.reportService.getGstReport(this.fromDate, this.toDate).subscribe({
      next: (rows) => {
        this.rows = rows;
        this.isLoading = false;
      },
      error: (err) => {
        this.loadError = 'Failed to load the ledger. Please check the server connection.';
        this.isLoading = false;
        console.error('Ledger load error:', err);
      }
    });
  }

  get totals() {
    return this.rows.reduce(
      (acc, r) => ({
        gross: acc.gross + r.grossRevenue,
        gst: acc.gst + r.gstAmount,
        net: acc.net + r.netIncome
      }),
      { gross: 0, gst: 0, net: 0 }
    );
  }
}
