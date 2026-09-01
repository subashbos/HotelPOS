import { Component, OnInit } from '@angular/core';
import { ReportService } from '../../../services/report.service';
import { PagedPurchaseReport } from '../../../models/report.model';
import { downloadBlob } from '../../../utils/download.util';
import { SupplierService } from '../../../services/supplier.service';
import { Supplier } from '../../../models/supplier.model';
import { PURCHASE_PAYMENT_TYPES } from '../purchases/purchases.component';

@Component({
  standalone: false,
  selector: 'app-purchase-report',
  templateUrl: './purchase-report.component.html',
})
export class PurchaseReportComponent implements OnInit {
  report: PagedPurchaseReport | null = null;
  suppliers: Supplier[] = [];
  readonly paymentTypes = PURCHASE_PAYMENT_TYPES;
  isLoading = false;
  loadError = '';
  isExporting = false;

  page = 1;
  pageSize = 20;
  fromDate = '';
  toDate = '';
  supplierId: number | null = null;
  itemName = '';
  paymentType = '';
  invoiceNo = '';

  constructor(
    private readonly reportService: ReportService,
    private readonly supplierService: SupplierService
  ) {}

  ngOnInit(): void {
    this.load();
    this.supplierService.getSuppliers().subscribe({
      next: (suppliers) => (this.suppliers = suppliers),
      error: (err) => console.error('Suppliers load error:', err)
    });
  }

  private get filters() {
    return {
      supplierId: this.supplierId || undefined,
      itemName: this.itemName || undefined,
      paymentType: this.paymentType || undefined,
      invoiceNo: this.invoiceNo || undefined
    };
  }

  load(): void {
    this.isLoading = true;
    this.loadError = '';
    this.reportService.getPurchaseReport(
      this.page, this.pageSize, this.fromDate || undefined, this.toDate || undefined, this.filters
    ).subscribe({
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

  applyFilters(): void {
    this.page = 1;
    this.load();
  }

  clearFilters(): void {
    this.supplierId = null;
    this.itemName = '';
    this.paymentType = '';
    this.invoiceNo = '';
    this.applyFilters();
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

  export(): void {
    this.isExporting = true;
    this.reportService.exportPurchaseReport(this.fromDate || undefined, this.toDate || undefined, this.filters).subscribe({
      next: (blob) => {
        downloadBlob(blob, `Purchase_Report_${Date.now()}.xlsx`);
        this.isExporting = false;
      },
      error: (err) => {
        this.isExporting = false;
        console.error('Purchase report export error:', err);
      }
    });
  }
}
