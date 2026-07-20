export interface PurchaseItem {
  id: number;
  itemId: number;
  itemName: string;
  quantity: number;
  unitPrice: number;
  taxPercentage: number;
  discount: number;
  total: number;
}

export interface Purchase {
  sNo: number;
  id: number;
  supplierId: number;
  supplierName?: string;
  invoiceNumber: string;
  purchaseDate: string;
  paymentType: string;
  notes?: string;
  subtotal: number;
  totalTax: number;
  totalDiscount: number;
  grandTotal: number;
  items: PurchaseItem[];
}

export interface SavePurchaseItemRequest {
  itemId: number;
  itemName: string;
  quantity: number;
  unitPrice: number;
  taxPercentage: number;
  discount: number;
}

export interface SavePurchaseRequest {
  supplierId: number;
  invoiceNumber: string;
  purchaseDate: string;
  paymentType: string;
  notes?: string;
  totalDiscount: number;
  items: SavePurchaseItemRequest[];
}
