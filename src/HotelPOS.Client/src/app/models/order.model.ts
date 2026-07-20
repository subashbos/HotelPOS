/** Display labels for order type buttons. Spaces are stripped before sending to the API (see billing.component.ts). */
export const ORDER_TYPE_LABELS = ['Dine In', 'Takeaway', 'Online'] as const;

export const PAYMENT_MODES = {
  Cash: 'Cash',
  Card: 'Card',
  Upi: 'UPI',
} as const;

export interface CreateOrderItemRequest {
  itemId: number;
  itemName: string;
  quantity: number;
  price: number;
  taxPercentage: number;
  total: number;
}

export interface CreateOrderRequest {
  items: CreateOrderItemRequest[];
  tableNumber: number;
  discount: number;
  paymentMode: string;
  customerName?: string;
  customerPhone?: string;
  customerGstin?: string;
  orderType: string;
}
