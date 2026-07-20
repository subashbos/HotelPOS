export interface Customer {
  id: number;
  name: string;
  phone?: string;
  email?: string;
  address?: string;
  gstin?: string;
  notes?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface SaveCustomerRequest {
  id: number;
  name: string;
  phone?: string;
  email?: string;
  address?: string;
  gstin?: string;
  notes?: string;
}

export interface CustomerOrderSummary {
  orderId: number;
  invoiceNumber?: string;
  createdAt: string;
  totalAmount: number;
  status: string;
  orderType: string;
}

export interface CustomerHistory {
  customerId: number;
  customerName: string;
  totalOrders: number;
  totalSpent: number;
  firstOrderDate?: string;
  lastOrderDate?: string;
  orders: CustomerOrderSummary[];
}
