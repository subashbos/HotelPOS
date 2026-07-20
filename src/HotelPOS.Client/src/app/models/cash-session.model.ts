export interface CashSession {
  sNo: number;
  id: number;
  openedAt: string;
  closedAt?: string;
  openingBalance: number;
  closingBalance?: number;
  actualCash?: number;
  openedBy: string;
  closedBy?: string;
  status: 'Open' | 'Closed';
  notes?: string;
}
