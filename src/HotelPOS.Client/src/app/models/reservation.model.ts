export interface Reservation {
  id: number;
  tableId: number;
  tableName?: string;
  customerId?: number;
  customerName?: string;
  customerPhone?: string;
  reservationDate: string;
  startTime: string;
  endTime: string;
  partySize: number;
  status: string;
  notes?: string;
  createdAt: string;
}

export interface SaveReservationRequest {
  tableId: number;
  customerId?: number;
  customerName?: string;
  customerPhone?: string;
  reservationDate: string;
  startTime: string;
  endTime: string;
  partySize: number;
  notes?: string;
}
