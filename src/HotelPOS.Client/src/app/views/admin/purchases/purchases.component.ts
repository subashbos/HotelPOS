import { Component, OnInit } from '@angular/core';
import { PurchaseService } from '../../../services/purchase.service';
import { ItemService } from '../../../services/item.service';
import { Purchase, SavePurchaseItemRequest } from '../../../models/purchase.model';
import { Supplier } from '../../../models/supplier.model';
import { Item } from '../../../models/item.model';

interface PurchaseLine extends SavePurchaseItemRequest {
  total: number;
}

export const PURCHASE_PAYMENT_TYPES = ['Cash', 'Credit', 'UPI'] as const;

@Component({
  standalone: false,
  selector: 'app-purchases',
  templateUrl: './purchases.component.html',
})
export class PurchasesComponent implements OnInit {
  purchases: Purchase[] = [];
  suppliers: Supplier[] = [];
  catalogItems: Item[] = [];

  isLoading = false;
  loadError = '';
  actionError = '';
  isSaving = false;

  readonly paymentTypes = PURCHASE_PAYMENT_TYPES;

  // ── Entry form ──
  showForm = false;
  supplierId: number | null = null;
  invoiceNumber = '';
  purchaseDate = new Date().toISOString().substring(0, 10);
  paymentType: string = PURCHASE_PAYMENT_TYPES[0];
  notes = '';
  extraDiscount = 0;
  lines: PurchaseLine[] = [];

  // ── Line entry ──
  selectedItemId: number | null = null;
  lineQuantity = 1;
  lineUnitPrice = 0;
  lineTaxPercentage = 0;
  lineDiscount = 0;

  constructor(
    private readonly purchaseService: PurchaseService,
    private readonly itemService: ItemService
  ) {}

  ngOnInit(): void {
    this.loadPurchases();
    this.purchaseService.getSuppliers().subscribe({
      next: (suppliers) => (this.suppliers = suppliers),
      error: (err) => console.error('Suppliers load error:', err)
    });
    this.itemService.getItems().subscribe({
      next: (items) => (this.catalogItems = items),
      error: (err) => console.error('Items load error:', err)
    });
  }

  loadPurchases(): void {
    this.isLoading = true;
    this.loadError = '';
    this.purchaseService.getPurchases().subscribe({
      next: (purchases) => {
        this.purchases = purchases.sort((a, b) => (a.purchaseDate < b.purchaseDate ? 1 : -1));
        this.isLoading = false;
      },
      error: (err) => {
        this.loadError = 'Failed to load purchases. Please check the server connection.';
        this.isLoading = false;
        console.error('Purchases load error:', err);
      }
    });
  }

  openForm(): void {
    this.supplierId = null;
    this.invoiceNumber = '';
    this.purchaseDate = new Date().toISOString().substring(0, 10);
    this.paymentType = PURCHASE_PAYMENT_TYPES[0];
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

  savePurchase(): void {
    if (!this.supplierId || !this.invoiceNumber.trim() || this.lines.length === 0 || this.isSaving) return;
    this.isSaving = true;
    this.actionError = '';

    this.purchaseService.createPurchase({
      supplierId: this.supplierId,
      invoiceNumber: this.invoiceNumber.trim(),
      purchaseDate: this.purchaseDate,
      paymentType: this.paymentType,
      notes: this.notes || undefined,
      totalDiscount: this.extraDiscount,
      items: this.lines.map(({ itemId, itemName, quantity, unitPrice, taxPercentage, discount }) => ({
        itemId, itemName, quantity, unitPrice, taxPercentage, discount
      }))
    }).subscribe({
      next: () => {
        this.isSaving = false;
        this.closeForm();
        this.loadPurchases();
      },
      error: (err) => {
        this.isSaving = false;
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to save purchase.';
        console.error('Purchase save error:', err);
      }
    });
  }

  trackByPurchaseId(_index: number, purchase: Purchase): number {
    return purchase.id;
  }
}
