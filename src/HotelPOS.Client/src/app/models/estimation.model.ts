export interface EstimationItem {
  id: number;
  itemId: number;
  itemName: string;
  quantity: number;
  unitPrice: number;
  taxPercentage: number;
  discount: number;
  total: number;
}

export interface Estimation {
  id: number;
  estimationNumber: string;
  estimationDate: string;
  validUntil?: string;
  customerId?: number;
  customerName?: string;
  customerPhone?: string;
  status: string;
  notes?: string;
  subtotal: number;
  totalTax: number;
  totalDiscount: number;
  grandTotal: number;
  convertedOrderId?: number;
  items: EstimationItem[];
}

export interface SaveEstimationItemRequest {
  itemId: number;
  itemName: string;
  quantity: number;
  unitPrice: number;
  taxPercentage: number;
  discount: number;
}

export interface SaveEstimationRequest {
  estimationNumber: string;
  estimationDate: string;
  validUntil?: string;
  customerId?: number;
  customerName?: string;
  customerPhone?: string;
  status?: string;
  notes?: string;
  totalDiscount: number;
  items: SaveEstimationItemRequest[];
}

export interface ConvertEstimationResult {
  orderId: number;
}
