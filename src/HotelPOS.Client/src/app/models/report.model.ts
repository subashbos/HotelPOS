export interface PaymentModeSalesRow { sNo: number; paymentMode: string; revenue: number; orderCount: number; percentage: number; }
export interface CategorySalesRow { sNo: number; categoryName: string; revenue: number; percentage: number; }
export interface TableSalesRow { sNo: number; tableNumber: number; orderCount: number; totalRevenue: number; }
export interface RecentOrderRow {
  sNo: number; orderId: number; invoiceNumber?: string; tableNumber: number; createdAt: string;
  total: number; discountAmount: number; itemCount: number; paymentMode: string; orderType: string;
  status: string; customerName?: string; customerPhone?: string; customerGstin?: string;
}

export interface SalesReport {
  totalRevenue: number;
  totalOrders: number;
  averageOrderValue: number;
  mostPopularItem: string;
  salesByTable: TableSalesRow[];
  recentOrders: RecentOrderRow[];
  salesByCategory: CategorySalesRow[];
  salesByPaymentMode: PaymentModeSalesRow[];
}

export interface ItemReportRow { sNo: number; itemName: string; totalQtySold: number; totalRevenue: number; unitPrice: number; }
export interface GstReportRow { sNo: number; date: string; orderCount: number; grossRevenue: number; gstAmount: number; netIncome: number; }
export interface MonthlySalesChart { monthName: string; revenue: number; }

export interface PurchaseReportRow {
  sNo: number; purchaseId: number; purchaseDate: string; invoiceNumber: string; supplierName: string;
  itemName: string; quantity: number; unitPrice: number; taxAmount: number; discount: number;
  totalAmount: number; paymentType: string;
}
export interface PagedPurchaseReport {
  items: PurchaseReportRow[];
  totalCount: number;
  totalPurchases: number;
  totalTax: number;
  totalDiscount: number;
  totalQty: number;
}

export interface ProfitMarginSummary {
  totalRevenue: number; totalCogs: number; grossProfit: number; totalExpenses: number;
  netProfit: number; marginPercentage: number; foodCostPercentage: number;
}
export interface ItemMarginRow {
  sNo: number; itemName: string; categoryName: string; quantitySold: number; unitPrice: number;
  costPrice: number; totalRevenue: number; totalCogs: number; profit: number; marginPercentage: number;
  recommendation: string;
}
export interface WastageReasonRow { sNo: number; reason: string; quantity: number; cost: number; percentage: number; }
export interface WastageItemRow { sNo: number; id: number; itemName: string; quantity: number; reason: string; wastedAt: string; totalCost: number; notes?: string; }
export interface WastageSummary {
  totalWastageCost: number; totalWastageQty: number;
  reasonsBreakdown: WastageReasonRow[]; recentWastage: WastageItemRow[];
}
export interface LowStockAlert {
  sNo: number; itemId: number; itemName: string; currentStock: number; minThreshold: number;
  dailyConsumptionRate: number; daysRemaining: number; alertLevel: string;
}
export interface MonthlyTrend { monthName: string; revenue: number; grossProfit: number; netProfit: number; }
