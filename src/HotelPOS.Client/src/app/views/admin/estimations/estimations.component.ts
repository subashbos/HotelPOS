import { Component, OnInit } from '@angular/core';
import { EstimationService } from '../../../services/estimation.service';
import { ItemService } from '../../../services/item.service';
import { CustomerService } from '../../../services/customer.service';
import { Estimation, SaveEstimationItemRequest } from '../../../models/estimation.model';
import { Customer } from '../../../models/customer.model';
import { Item } from '../../../models/item.model';

interface EstimationLine extends SaveEstimationItemRequest {
  total: number;
}

export const ESTIMATION_STATUSES = ['Draft', 'Sent', 'Accepted', 'Rejected', 'Converted', 'Expired'] as const;

@Component({
  standalone: false,
  selector: 'app-estimations',
  templateUrl: './estimations.component.html',
})
export class EstimationsComponent implements OnInit {
  estimations: Estimation[] = [];
  customers: Customer[] = [];
  catalogItems: Item[] = [];

  isLoading = false;
  loadError = '';
  actionError = '';
  isSaving = false;
  convertingId: number | null = null;
  statusUpdatingId: number | null = null;

  private static readonly NEXT_STATUSES: Record<string, string[]> = {
    Draft: ['Sent', 'Rejected'],
    Sent: ['Accepted', 'Rejected'],
    Accepted: ['Rejected'],
    Rejected: [],
    Converted: [],
    Expired: []
  };

  readonly statuses = ESTIMATION_STATUSES;

  // ── Entry form ──
  showForm = false;
  estimationNumber = '';
  estimationDate = new Date().toISOString().substring(0, 10);
  validUntil = '';
  customerId: number | null = null;
  customerName = '';
  customerPhone = '';
  notes = '';
  extraDiscount = 0;
  lines: EstimationLine[] = [];

  // ── Line entry ──
  selectedItemId: number | null = null;
  lineQuantity = 1;
  lineUnitPrice = 0;
  lineTaxPercentage = 0;
  lineDiscount = 0;

  constructor(
    private readonly estimationService: EstimationService,
    private readonly itemService: ItemService,
    private readonly customerService: CustomerService
  ) {}

  ngOnInit(): void {
    this.loadEstimations();
    this.customerService.getCustomers().subscribe({
      next: (customers) => (this.customers = customers),
      error: (err) => console.error('Customers load error:', err)
    });
    this.itemService.getItems().subscribe({
      next: (items) => (this.catalogItems = items),
      error: (err) => console.error('Items load error:', err)
    });
  }

  loadEstimations(): void {
    this.isLoading = true;
    this.loadError = '';
    this.estimationService.getEstimations().subscribe({
      next: (estimations) => {
        this.estimations = estimations.sort((a, b) => b.estimationDate.localeCompare(a.estimationDate));
        this.isLoading = false;
      },
      error: (err) => {
        this.loadError = 'Failed to load estimations. Please check the server connection.';
        this.isLoading = false;
        console.error('Estimations load error:', err);
      }
    });
  }

  openForm(): void {
    this.estimationNumber = '';
    this.estimationDate = new Date().toISOString().substring(0, 10);
    this.validUntil = '';
    this.customerId = null;
    this.customerName = '';
    this.customerPhone = '';
    this.notes = '';
    this.extraDiscount = 0;
    this.lines = [];
    this.actionError = '';
    this.resetLineEntry();
    this.showForm = true;
  }

  closeForm(): void {
    this.showForm = false;
  }

  resetLineEntry(): void {
    this.selectedItemId = null;
    this.lineQuantity = 1;
    this.lineUnitPrice = 0;
    this.lineTaxPercentage = 0;
    this.lineDiscount = 0;
  }

  onCustomerSelected(): void {
    const customer = this.customers.find((c) => c.id === this.customerId);
    if (customer) {
      this.customerName = customer.name;
      this.customerPhone = customer.phone || '';
    }
  }

  onItemSelected(): void {
    const item = this.catalogItems.find((i) => i.id === this.selectedItemId);
    if (item) {
      this.lineUnitPrice = Number(item.price);
      this.lineTaxPercentage = Number(item.taxPercentage);
    }
  }

  addLine(): void {
    const item = this.catalogItems.find((i) => i.id === this.selectedItemId);
    if (!item || this.lineQuantity <= 0 || this.lineUnitPrice < 0) return;

    const total = Math.round((this.lineQuantity * this.lineUnitPrice) * (1 + this.lineTaxPercentage / 100) * 100) / 100 - this.lineDiscount;
    this.lines.push({
      itemId: item.id,
      itemName: item.name,
      quantity: this.lineQuantity,
      unitPrice: this.lineUnitPrice,
      taxPercentage: this.lineTaxPercentage,
      discount: this.lineDiscount,
      total: Math.max(total, 0)
    });
    this.resetLineEntry();
  }

  removeLine(index: number): void {
    this.lines.splice(index, 1);
  }

  get subtotal(): number {
    return this.lines.reduce((s, l) => s + l.quantity * l.unitPrice, 0);
  }

  get taxTotal(): number {
    return this.lines.reduce((s, l) => s + Math.round(l.quantity * l.unitPrice * l.taxPercentage / 100 * 100) / 100, 0);
  }

  get grandTotal(): number {
    const lineDiscounts = this.lines.reduce((s, l) => s + l.discount, 0);
    return Math.max(this.subtotal + this.taxTotal - lineDiscounts - this.extraDiscount, 0);
  }

  saveEstimation(): void {
    if (!this.estimationNumber.trim() || this.lines.length === 0 || this.isSaving) return;
    this.isSaving = true;
    this.actionError = '';

    this.estimationService.createEstimation({
      estimationNumber: this.estimationNumber.trim(),
      estimationDate: this.estimationDate,
      validUntil: this.validUntil || undefined,
      customerId: this.customerId || undefined,
      customerName: this.customerName || undefined,
      customerPhone: this.customerPhone || undefined,
      status: 'Draft',
      notes: this.notes || undefined,
      totalDiscount: this.extraDiscount,
      items: this.lines.map(({ itemId, itemName, quantity, unitPrice, taxPercentage, discount }) => ({
        itemId, itemName, quantity, unitPrice, taxPercentage, discount
      }))
    }).subscribe({
      next: () => {
        this.isSaving = false;
        this.closeForm();
        this.loadEstimations();
      },
      error: (err) => {
        this.isSaving = false;
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to save estimation.';
        console.error('Estimation save error:', err);
      }
    });
  }

  nextStatuses(estimation: Estimation): string[] {
    return EstimationsComponent.NEXT_STATUSES[estimation.status] ?? [];
  }

  changeStatus(estimation: Estimation, newStatus: string): void {
    if (!newStatus || this.statusUpdatingId !== null) return;
    this.statusUpdatingId = estimation.id;
    this.actionError = '';

    // The server re-derives TotalDiscount as (sum of line discounts) + request.totalDiscount, so
    // only the "extra" portion on top of line-level discounts must be sent back here - reusing
    // estimation.totalDiscount as-is would double-count the line discounts already stored on it.
    const lineDiscountTotal = estimation.items.reduce((s, i) => s + i.discount, 0);
    const extraDiscount = estimation.totalDiscount - lineDiscountTotal;

    this.estimationService.updateEstimation(estimation.id, {
      estimationNumber: estimation.estimationNumber,
      estimationDate: estimation.estimationDate,
      validUntil: estimation.validUntil,
      customerId: estimation.customerId,
      customerName: estimation.customerName,
      customerPhone: estimation.customerPhone,
      status: newStatus,
      notes: estimation.notes,
      totalDiscount: extraDiscount,
      items: estimation.items.map(({ itemId, itemName, quantity, unitPrice, taxPercentage, discount }) => ({
        itemId, itemName, quantity, unitPrice, taxPercentage, discount
      }))
    }).subscribe({
      next: () => {
        this.statusUpdatingId = null;
        this.loadEstimations();
      },
      error: (err) => {
        this.statusUpdatingId = null;
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to update estimation status.';
        console.error('Estimation status update error:', err);
      }
    });
  }

  convertToOrder(estimation: Estimation): void {
    if (estimation.status !== 'Accepted' || this.convertingId !== null) return;
    this.convertingId = estimation.id;
    this.actionError = '';

    this.estimationService.convertToOrder(estimation.id).subscribe({
      next: () => {
        this.convertingId = null;
        this.loadEstimations();
      },
      error: (err) => {
        this.convertingId = null;
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to convert estimation to an order.';
        console.error('Estimation conversion error:', err);
      }
    });
  }

  canConvert(estimation: Estimation): boolean {
    return estimation.status === 'Accepted';
  }

  trackByEstimationId(_index: number, estimation: Estimation): number {
    return estimation.id;
  }
}
