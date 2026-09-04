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

const DEFAULT_RANGE_START_MIN = 9 * 60;
const DEFAULT_RANGE_END_MIN = 23 * 60;
const PX_PER_HOUR = 72;
const SNAP_MINUTES = 30;

function timeToMinutes(value: string): number {
  const [h, m] = value.split(':').map((p) => parseInt(p, 10));
  return (h || 0) * 60 + (m || 0);
}

function minutesToTime(value: number): string {
  const h = Math.floor(value / 60) % 24;
  const m = value % 60;
  return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`;
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

  // ── Scheduler view ──
  viewMode: 'scheduler' | 'list' = 'scheduler';
  pxPerHour = PX_PER_HOUR;
  selectedBlockId: number | null = null;

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
        this.reservations = [...reservations].sort((a, b) => a.startTime.localeCompare(b.startTime));
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
    this.selectedBlockId = null;
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

  /** Same as openForm(), but prefilled from a scheduler click instead of cleared. */
  openFormAt(table: DiningTable, startMinutes: number): void {
    const endMinutes = Math.min(startMinutes + 60, this.rangeEndMinutes);
    this.formTableId = table.id;
    this.formCustomerId = null;
    this.formCustomerName = '';
    this.formCustomerPhone = '';
    this.formDate = this.selectedDate;
    this.formStartTime = minutesToTime(startMinutes);
    this.formEndTime = minutesToTime(endMinutes);
    this.formPartySize = 2;
    this.formNotes = '';
    this.actionError = '';
    this.showForm = true;
  }

  closeForm(): void {
    this.showForm = false;
  }

  // ── Scheduler view helpers ──

  get rangeStartMinutes(): number {
    const starts = this.reservations.map((r) => timeToMinutes(r.startTime));
    const earliest = starts.length ? Math.min(...starts) : DEFAULT_RANGE_START_MIN;
    return Math.floor(Math.min(DEFAULT_RANGE_START_MIN, earliest) / 60) * 60;
  }

  get rangeEndMinutes(): number {
    const ends = this.reservations.map((r) => timeToMinutes(r.endTime));
    const latest = ends.length ? Math.max(...ends) : DEFAULT_RANGE_END_MIN;
    return Math.ceil(Math.max(DEFAULT_RANGE_END_MIN, latest) / 60) * 60;
  }

  get timelineWidthPx(): number {
    return ((this.rangeEndMinutes - this.rangeStartMinutes) / 60) * this.pxPerHour;
  }

  get hourMarks(): { label: string; left: number }[] {
    const marks: { label: string; left: number }[] = [];
    for (let m = this.rangeStartMinutes; m <= this.rangeEndMinutes; m += 60) {
      marks.push({ label: minutesToTime(m), left: ((m - this.rangeStartMinutes) / 60) * this.pxPerHour });
    }
    return marks;
  }

  get nowLineLeft(): number | null {
    if (this.selectedDate !== today()) return null;
    const now = new Date();
    const minutes = now.getHours() * 60 + now.getMinutes();
    if (minutes < this.rangeStartMinutes || minutes > this.rangeEndMinutes) return null;
    return ((minutes - this.rangeStartMinutes) / 60) * this.pxPerHour;
  }

  reservationsForTable(tableId: number): Reservation[] {
    return this.reservations.filter((r) => r.tableId === tableId);
  }

  blockStyle(r: Reservation): { left: string; width: string } {
    const start = timeToMinutes(r.startTime);
    const end = timeToMinutes(r.endTime);
    const left = ((start - this.rangeStartMinutes) / 60) * this.pxPerHour;
    const width = Math.max(((end - start) / 60) * this.pxPerHour, 24);
    return { left: `${left}px`, width: `${width}px` };
  }

  onTimelineClick(event: MouseEvent, table: DiningTable): void {
    this.selectedBlockId = null;
    const target = event.currentTarget as HTMLElement;
    const rect = target.getBoundingClientRect();
    const offsetX = event.clientX - rect.left;
    const rawMinutes = this.rangeStartMinutes + (offsetX / this.pxPerHour) * 60;
    const snapped = Math.round(rawMinutes / SNAP_MINUTES) * SNAP_MINUTES;
    this.openFormAt(table, Math.min(Math.max(snapped, this.rangeStartMinutes), this.rangeEndMinutes - 30));
  }

  onBlockClick(event: MouseEvent, reservation: Reservation): void {
    event.stopPropagation();
    this.selectedBlockId = this.selectedBlockId === reservation.id ? null : reservation.id;
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
        this.selectedBlockId = null;
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
        this.selectedBlockId = null;
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
