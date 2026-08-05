import { Component, OnInit } from '@angular/core';
import { ReservationService } from '../../../services/reservation.service';
import { TableService } from '../../../services/table.service';
import { CustomerService } from '../../../services/customer.service';
import { Reservation } from '../../../models/reservation.model';
import { DiningTable } from '../../../models/table.model';
import { Customer } from '../../../models/customer.model';

const NEXT_STATUSES: Record<string, string[]> = {
  Reserved: ['CheckedIn', 'Cancelled', 'NoShow'],
  CheckedIn: ['Completed', 'Cancelled'],
  Completed: [],
  Cancelled: [],
  NoShow: []
};

function today(): string {
  return new Date().toISOString().substring(0, 10);
}

@Component({
  standalone: false,
  selector: 'app-reservations',
  templateUrl: './reservations.component.html',
})
export class ReservationsComponent implements OnInit {
  reservations: Reservation[] = [];
  tables: DiningTable[] = [];
  customers: Customer[] = [];

  isLoading = false;
  loadError = '';
  actionError = '';
  isSaving = false;
  statusUpdatingId: number | null = null;

  selectedDate = today();

  // ── Entry form ──
  showForm = false;
  formTableId: number | null = null;
  formCustomerId: number | null = null;
  formCustomerName = '';
  formCustomerPhone = '';
  formDate = today();
  formStartTime = '';
  formEndTime = '';
  formPartySize = 2;
  formNotes = '';

  constructor(
    private readonly reservationService: ReservationService,
    private readonly tableService: TableService,
    private readonly customerService: CustomerService
  ) {}

  ngOnInit(): void {
    this.loadReservations();
    this.tableService.getTables().subscribe({
      next: (tables) => (this.tables = tables.filter((t) => t.isActive && !t.isDeleted)),
      error: (err) => console.error('Tables load error:', err)
    });
    this.customerService.getCustomers().subscribe({
      next: (customers) => (this.customers = customers),
      error: (err) => console.error('Customers load error:', err)
    });
  }

  loadReservations(): void {
    this.isLoading = true;
    this.loadError = '';
    this.reservationService.getReservations(this.selectedDate).subscribe({
      next: (reservations) => {
        this.reservations = reservations.sort((a, b) => a.startTime.localeCompare(b.startTime));
        this.isLoading = false;
      },
      error: (err) => {
        this.loadError = 'Failed to load reservations. Please check the server connection.';
        this.isLoading = false;
        console.error('Reservations load error:', err);
      }
    });
  }

  onDateChanged(): void {
    this.loadReservations();
  }

  openForm(): void {
    this.formTableId = null;
    this.formCustomerId = null;
    this.formCustomerName = '';
    this.formCustomerPhone = '';
    this.formDate = this.selectedDate;
    this.formStartTime = '';
    this.formEndTime = '';
    this.formPartySize = 2;
    this.formNotes = '';
    this.actionError = '';
    this.showForm = true;
  }

  closeForm(): void {
    this.showForm = false;
  }

  onCustomerSelected(): void {
    const customer = this.customers.find((c) => c.id === this.formCustomerId);
    if (customer) {
      this.formCustomerName = customer.name;
      this.formCustomerPhone = customer.phone || '';
    }
  }

  saveReservation(): void {
    if (!this.formTableId || !this.formDate || !this.formStartTime || !this.formEndTime || this.formPartySize <= 0 || this.isSaving) return;
    this.isSaving = true;
    this.actionError = '';

    this.reservationService.createReservation({
      tableId: this.formTableId,
      customerId: this.formCustomerId || undefined,
      customerName: this.formCustomerName || undefined,
      customerPhone: this.formCustomerPhone || undefined,
      reservationDate: this.formDate,
      startTime: `${this.formStartTime}:00`,
      endTime: `${this.formEndTime}:00`,
      partySize: this.formPartySize,
      notes: this.formNotes || undefined
    }).subscribe({
      next: () => {
        this.isSaving = false;
        this.closeForm();
        this.loadReservations();
      },
      error: (err) => {
        this.isSaving = false;
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to save reservation.';
        console.error('Reservation save error:', err);
      }
    });
  }

  nextStatuses(reservation: Reservation): string[] {
    return NEXT_STATUSES[reservation.status] ?? [];
  }

  changeStatus(reservation: Reservation, newStatus: string): void {
    if (!newStatus || this.statusUpdatingId !== null) return;
    this.statusUpdatingId = reservation.id;
    this.actionError = '';

    this.reservationService.changeStatus(reservation.id, newStatus).subscribe({
      next: () => {
        this.statusUpdatingId = null;
        this.loadReservations();
      },
      error: (err) => {
        this.statusUpdatingId = null;
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to update reservation status.';
        console.error('Reservation status update error:', err);
      }
    });
  }

  deleteReservation(reservation: Reservation): void {
    if (this.statusUpdatingId !== null) return;
    this.statusUpdatingId = reservation.id;
    this.actionError = '';

    this.reservationService.deleteReservation(reservation.id).subscribe({
      next: () => {
        this.statusUpdatingId = null;
        this.loadReservations();
      },
      error: (err) => {
        this.statusUpdatingId = null;
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to delete reservation.';
        console.error('Reservation delete error:', err);
      }
    });
  }

  formatTime(value: string): string {
    return value ? value.substring(0, 5) : '—';
  }

  trackByReservationId(_index: number, reservation: Reservation): number {
    return reservation.id;
  }
}
