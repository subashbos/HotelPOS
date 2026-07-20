export const EXPENSE_CATEGORIES = [
  'General', 'Salary', 'Rent', 'Raw Material', 'Utilities', 'Maintenance', 'Marketing', 'Miscellaneous'
] as const;

export interface Expense {
  sNo: number;
  id: number;
  date: string;
  title: string;
  description?: string;
  amount: number;
  category: string;
  paymentMode?: string;
  createdBy?: number;
  createdByUsername?: string;
}

export interface SaveExpenseRequest {
  id: number;
  date: string;
  title: string;
  description?: string;
  amount: number;
  category: string;
  paymentMode?: string;
}
