export interface ReceiptLineItem {
  itemName: string;
  price: number;
  quantity: number;
  total: number;
  taxPercentage: number;
}

export interface ReceiptData {
  orderId: number | string;
  createdAt: Date;
  items: ReceiptLineItem[];
  subtotal: number;
  discountAmount: number;
  totalAmount: number;
  paymentMode: string;
  customerName?: string;
  customerPhone?: string;
  customerGstin?: string;
}

export interface KotLineItem {
  itemName: string;
  quantity: number;
}

export interface KotData {
  tableNumber: number;
  items: KotLineItem[];
}
