import { Component, OnInit } from '@angular/core';
import { ReportService } from '../../../services/report.service';
import { GstR1B2cSummaryRow, GstR1Row, HsnSummaryRow } from '../../../models/report.model';

function firstOfMonth(): string {
  const d = new Date();
  return new Date(d.getFullYear(), d.getMonth(), 1).toISOString().substring(0, 10);
}
function today(): string {
  return new Date().toISOString().substring(0, 10);
}

type Tab = 'b2b' | 'b2c' | 'hsn';

@Component({
  standalone: false,
  selector: 'app-gstr1-report',
  templateUrl: './gstr1-report.component.html',
})
export class Gstr1ReportComponent implements OnInit {
  activeTab: Tab = 'b2b';

  b2bRows: GstR1Row[] = [];
  b2cSummary: GstR1B2cSummaryRow[] = [];
  hsnSummary: HsnSummaryRow[] = [];

  isLoading = false;
  loadError = '';

  fromDate = firstOfMonth();
  toDate = today();

  constructor(private readonly reportService: ReportService) {}

  ngOnInit(): void {
    this.load();
  }

  setTab(tab: Tab): void {
    this.activeTab = tab;
  }

  load(): void {
    this.isLoading = true;
    this.loadError = '';
    this.reportService.getGstR1Report(this.fromDate, this.toDate).subscribe({
      next: (report) => {
        this.b2bRows = report.b2BRows;
        this.b2cSummary = report.b2cSummary;
        this.hsnSummary = report.hsnSummary;
        this.isLoading = false;
      },
      error: (err) => {
        this.loadError = 'Failed to load the GSTR-1 report. Please check the server connection.';
        this.isLoading = false;
        console.error('GSTR-1 report load error:', err);
      }
    });
  }

  get distinctRecipients(): number {
    return new Set(this.b2bRows.map(r => r.gstin)).size;
  }

  get distinctInvoices(): number {
    return new Set(this.b2bRows.map(r => r.invoiceNumber)).size;
  }

  get b2bTaxableValue(): number {
    return this.b2bRows.reduce((s, r) => s + r.taxableValue, 0);
  }

  get b2bTaxAmount(): number {
    return this.b2bRows.reduce((s, r) => s + r.cgst + r.sgst + r.igst, 0);
  }

  get b2cInvoiceCount(): number {
    return this.b2cSummary.reduce((s, r) => s + r.invoiceCount, 0);
  }

  get b2cTaxableValue(): number {
    return this.b2cSummary.reduce((s, r) => s + r.taxableValue, 0);
  }

  get b2cTaxAmount(): number {
    return this.b2cSummary.reduce((s, r) => s + r.totalTax, 0);
  }

  get hsnCodeCount(): number {
    return new Set(this.hsnSummary.map(r => r.hsnCode)).size;
  }

  get hsnTotalQuantity(): number {
    return this.hsnSummary.reduce((s, r) => s + r.totalQuantity, 0);
  }

  get hsnTaxableValue(): number {
    return this.hsnSummary.reduce((s, r) => s + r.taxableValue, 0);
  }
}
