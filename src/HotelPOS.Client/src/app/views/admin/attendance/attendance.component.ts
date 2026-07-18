import { Component, OnInit } from '@angular/core';
import { AttendanceService } from '../../../services/attendance.service';
import { EmployeeService } from '../../../services/employee.service';
import { Attendance, ATTENDANCE_STATUSES } from '../../../models/attendance.model';
import { Employee } from '../../../models/employee.model';

function firstOfMonth(): string {
  const d = new Date();
  return new Date(d.getFullYear(), d.getMonth(), 1).toISOString().substring(0, 10);
}

function today(): string {
  return new Date().toISOString().substring(0, 10);
}

@Component({
  standalone: false,
  selector: 'app-attendance',
  templateUrl: './attendance.component.html',
})
export class AttendanceComponent implements OnInit {
  employees: Employee[] = [];
  records: Attendance[] = [];

  isLoading = false;
  loadError = '';
  actionError = '';
  isSaving = false;

  readonly statuses = ATTENDANCE_STATUSES;

  selectedEmployeeId: number | null = null;
  fromDate = firstOfMonth();
  toDate = today();

  showForm = false;
  formDate = today();
  formStatus: string = ATTENDANCE_STATUSES[0];
  formCheckIn = '';
  formCheckOut = '';
  formRemarks = '';

  constructor(
    private readonly attendanceService: AttendanceService,
    private readonly employeeService: EmployeeService
  ) {}

  ngOnInit(): void {
    this.employeeService.getEmployees().subscribe({
      next: (employees) => {
        this.employees = employees;
        if (employees.length > 0) {
          this.selectedEmployeeId = employees[0].id;
          this.loadAttendance();
        }
      },
      error: (err) => console.error('Employees load error:', err)
    });
  }

  loadAttendance(): void {
    if (!this.selectedEmployeeId) return;
    this.isLoading = true;
    this.loadError = '';
    this.attendanceService.getAttendance(this.selectedEmployeeId, this.fromDate, this.toDate).subscribe({
      next: (records) => {
        this.records = records.sort((a, b) => (a.date < b.date ? 1 : -1));
        this.isLoading = false;
      },
      error: (err) => {
        this.loadError = 'Failed to load attendance. Please check the server connection.';
        this.isLoading = false;
        console.error('Attendance load error:', err);
      }
    });
  }

  openMarkForm(): void {
    this.formDate = today();
    this.formStatus = ATTENDANCE_STATUSES[0];
    this.formCheckIn = '';
    this.formCheckOut = '';
    this.formRemarks = '';
    this.actionError = '';
    this.showForm = true;
  }

  closeForm(): void {
    this.showForm = false;
  }

  markAttendance(): void {
    if (!this.selectedEmployeeId || this.isSaving) return;
    this.isSaving = true;
    this.actionError = '';

    this.attendanceService.markAttendance({
      id: 0,
      employeeId: this.selectedEmployeeId,
      date: this.formDate,
      checkInTime: this.formCheckIn ? `${this.formCheckIn}:00` : undefined,
      checkOutTime: this.formCheckOut ? `${this.formCheckOut}:00` : undefined,
      status: this.formStatus,
      remarks: this.formRemarks || undefined
    }).subscribe({
      next: () => {
        this.isSaving = false;
        this.closeForm();
        this.loadAttendance();
      },
      error: (err) => {
        this.isSaving = false;
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to mark attendance.';
        console.error('Mark attendance error:', err);
      }
    });
  }

  deleteRecord(record: Attendance): void {
    if (!confirm(`Delete attendance record for ${record.date}?`)) return;
    this.attendanceService.deleteAttendance(record.id).subscribe({
      next: () => this.loadAttendance(),
      error: (err) => {
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to delete record.';
        console.error('Delete attendance error:', err);
      }
    });
  }

  formatTime(value?: string): string {
    return value ? value.substring(0, 5) : '—';
  }

  trackByAttendanceId(_index: number, record: Attendance): number {
    return record.id;
  }
}
