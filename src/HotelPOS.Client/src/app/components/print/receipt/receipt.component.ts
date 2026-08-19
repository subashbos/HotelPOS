import { Component, Input } from '@angular/core';
import { ReceiptData } from '../../../models/print.model';
import { SystemSettings } from '../../../models/settings.model';

interface TaxGroupRow {
  halfRate: number;
  halfTax: number;
}

@Component({
  standalone: false,
  selector: 'app-receipt',
  templateUrl: './receipt.component.html'
})
export class ReceiptComponent {
  @Input() data!: ReceiptData;
  @Input() settings!: SystemSettings;

  get isThermal(): boolean {
    return this.settings?.receiptFormat === 'Thermal';
  }

  get receiptTitle(): string {
    return this.settings?.isCompositionScheme ? 'BILL OF SUPPLY' : 'TAX INVOICE';
  }

  get hasCustomer(): boolean {
    return !!(this.data.customerName || this.data.customerPhone || this.data.customerGstin);
  }

  get showGstBreakdown(): boolean {
    return !!this.settings?.showGstBreakdown && !this.settings?.isCompositionScheme;
  }

  get taxGroups(): TaxGroupRow[] {
    const groups = new Map<number, number>();
    for (const item of this.data.items) {
      if (!item.taxPercentage) continue;
      const groupSubtotal = item.price * item.quantity;
      groups.set(item.taxPercentage, (groups.get(item.taxPercentage) || 0) + groupSubtotal);
    }
    return [...groups.entries()]
      .sort(([a], [b]) => a - b)
      .map(([rate, groupSubtotal]) => {
        const groupTax = groupSubtotal * (rate / 100);
        return { halfRate: rate / 2, halfTax: groupTax / 2 };
      });
  }

  get roundOffDiff(): number {
    if (!this.settings?.enableRoundOff) return 0;
    const rounded = Math.round(this.data.totalAmount);
    return rounded - this.data.totalAmount;
  }

  get grandTotal(): number {
    return this.settings?.enableRoundOff
      ? Math.round(this.data.totalAmount)
      : this.data.totalAmount;
  }

  get separator(): string {
    return (this.isThermal ? '-' : '_').repeat(this.isThermal ? 38 : 86);
  }
}
