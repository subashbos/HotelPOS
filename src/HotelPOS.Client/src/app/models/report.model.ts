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

export interface ShiftClosureReport {
  sessionId: number; openedAt: string; closedAt?: string; openedBy: string; closedBy?: string;
  status: string; openingBalance: number; theoreticalClosingBalance: number; actualCashCounted: number;
  cashVariance: number; totalSales: number; cashSales: number; cardSales: number; upiSales: number;
  creditSales: number; totalOrdersCount: number;
}

export interface VoidDiscountAuditRow {
  sNo: number; orderId: number; invoiceNumber: string; timestamp: string; orderType: string;
  subtotal: number; discountAmount: number; totalAmount: number; status: string;
  refundReason?: string; voidReason?: string; customerOrCashierInfo: string;
}

export interface StaffPerformanceReport {
  sNo: number; staffId: number; staffName: string; role: string; ordersProcessedCount: number;
  totalRevenueGenerated: number; averageBillAmount: number; totalDiscountsGiven: number;
}

export interface StockValuationRow {
  sNo: number; itemId: number; itemName: string; categoryName: string; stockQuantity: number;
  costPrice: number; retailPrice: number; totalCostValue: number; totalRetailValue: number; abcCategory: string;
}

export interface StockValuationSummary {
  totalInventoryCostValue: number; totalInventoryRetailValue: number; totalTrackedItemsCount: number;
  highValueCategoryACount: number; mediumValueCategoryBCount: number; lowValueCategoryCCount: number;
  items: StockValuationRow[];
}

export interface ExpenseCategoryBreakdown {
  sNo: number; category: string; amount: number; percentageOfTotalExpenses: number;
}

export interface ProfitAndLossReport {
  periodFrom: string; periodTo: string; totalSalesRevenue: number; totalCostOfGoodsSold: number;
  grossProfit: number; grossProfitMarginPercentage: number; totalExpenses: number;
  expensesByCategory: ExpenseCategoryBreakdown[]; netOperatingProfit: number; netProfitMarginPercentage: number;
}

