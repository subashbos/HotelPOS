import { Component, OnInit } from '@angular/core';
import { LeaveService } from '../../../services/leave.service';
import { EmployeeService } from '../../../services/employee.service';
import { LeaveBalance, LeaveRequest, LeaveType } from '../../../models/leave.model';
import { Employee } from '../../../models/employee.model';

@Component({
  standalone: false,
  selector: 'app-leave',
  templateUrl: './leave.component.html',
})
export class LeaveComponent implements OnInit {
  employees: Employee[] = [];
  leaveTypes: LeaveType[] = [];
  balances: LeaveBalance[] = [];
  requests: LeaveRequest[] = [];

  isLoading = false;
  loadError = '';
  actionError = '';
  isSaving = false;

  selectedEmployeeId: number | null = null;
  approverEmployeeId: number | null = null;

  showForm = false;
  formLeaveTypeId: number | null = null;
  formFromDate = new Date().toISOString().substring(0, 10);
  formToDate = new Date().toISOString().substring(0, 10);
  formReason = '';

  rejectingRequestId: number | null = null;
  rejectReason = '';

  constructor(
    private readonly leaveService: LeaveService,
    private readonly employeeService: EmployeeService
  ) {}

  ngOnInit(): void {
    this.leaveService.getLeaveTypes().subscribe({
      next: (types) => (this.leaveTypes = types),
      error: (err) => console.error('Leave types load error:', err)
    });
    this.employeeService.getEmployees().subscribe({
      next: (employees) => {
        this.employees = employees;
        if (employees.length > 0) {
          this.selectedEmployeeId = employees[0].id;
          this.load();
        }
      },
      error: (err) => console.error('Employees load error:', err)
    });
    this.loadAllRequests();
  }

  load(): void {
    if (!this.selectedEmployeeId) return;
    this.isLoading = true;
    this.loadError = '';
    this.leaveService.getBalances(this.selectedEmployeeId).subscribe({
      next: (balances) => {
        this.balances = balances;
        this.isLoading = false;
      },
      error: (err) => {
        this.loadError = 'Failed to load leave balances. Please check the server connection.';
        this.isLoading = false;
        console.error('Leave balances load error:', err);
      }
    });
  }

  loadAllRequests(): void {
    this.leaveService.getRequests().subscribe({
      next: (requests) => (this.requests = requests.sort((a, b) => (a.appliedOn < b.appliedOn ? 1 : -1))),
      error: (err) => console.error('Leave requests load error:', err)
    });
  }

  openApplyForm(): void {
    this.formLeaveTypeId = this.leaveTypes.length > 0 ? this.leaveTypes[0].id : null;
    this.formFromDate = new Date().toISOString().substring(0, 10);
    this.formToDate = this.formFromDate;
    this.formReason = '';
    this.actionError = '';
    this.showForm = true;
  }

  closeForm(): void {
    this.showForm = false;
  }

  applyLeave(): void {
    if (!this.selectedEmployeeId || !this.formLeaveTypeId || this.isSaving) return;
    this.isSaving = true;
    this.actionError = '';

    this.leaveService.applyLeave({
      employeeId: this.selectedEmployeeId,
      leaveTypeId: this.formLeaveTypeId,
      fromDate: this.formFromDate,
      toDate: this.formToDate,
      reason: this.formReason || undefined
    }).subscribe({
      next: () => {
        this.isSaving = false;
        this.closeForm();
        this.load();
        this.loadAllRequests();
      },
      error: (err) => {
        this.isSaving = false;
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to apply leave.';
        console.error('Apply leave error:', err);
      }
    });
  }

  approve(request: LeaveRequest): void {
    if (!this.approverEmployeeId) {
      this.actionError = 'Select an approving employee first.';
      return;
    }
    this.leaveService.approveLeave(request.id, this.approverEmployeeId).subscribe({
      next: () => {
        this.load();
        this.loadAllRequests();
      },
      error: (err) => {
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to approve leave.';
        console.error('Approve leave error:', err);
      }
    });
  }

  startReject(request: LeaveRequest): void {
    this.rejectingRequestId = request.id;
    this.rejectReason = '';
  }

  cancelReject(): void {
    this.rejectingRequestId = null;
  }

  confirmReject(request: LeaveRequest): void {
    if (!this.approverEmployeeId) {
      this.actionError = 'Select an approving employee first.';
      return;
    }
    if (!this.rejectReason.trim()) return;
    this.leaveService.rejectLeave(request.id, this.approverEmployeeId, this.rejectReason.trim()).subscribe({
      next: () => {
        this.rejectingRequestId = null;
        this.load();
        this.loadAllRequests();
      },
      error: (err) => {
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to reject leave.';
        console.error('Reject leave error:', err);
      }
    });
  }

  trackByBalanceId(_index: number, balance: LeaveBalance): number {
    return balance.id;
  }

  trackByRequestId(_index: number, request: LeaveRequest): number {
    return request.id;
  }
}
