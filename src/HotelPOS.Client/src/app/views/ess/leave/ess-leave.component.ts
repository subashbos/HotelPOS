import { Component, OnInit } from '@angular/core';
import { EssService } from '../../../services/ess.service';
import { LeaveBalance, LeaveRequest, LeaveType } from '../../../models/leave.model';

@Component({
  standalone: false,
  selector: 'app-ess-leave',
  templateUrl: './ess-leave.component.html',
})
export class EssLeaveComponent implements OnInit {
  leaveTypes: LeaveType[] = [];
  balances: LeaveBalance[] = [];
  requests: LeaveRequest[] = [];

  isLoading = false;
  loadError = '';
  actionError = '';
  isSaving = false;

  showForm = false;
  formLeaveTypeId: number | null = null;
  formFromDate = new Date().toISOString().substring(0, 10);
  formToDate = new Date().toISOString().substring(0, 10);
  formReason = '';

  constructor(private readonly essService: EssService) {}

  ngOnInit(): void {
    this.essService.getLeaveTypes().subscribe({
      next: (types) => (this.leaveTypes = types),
      error: (err) => console.error('Leave types load error:', err)
    });
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.loadError = '';

    this.essService.getLeaveBalances().subscribe({
      next: (balances) => {
        this.balances = balances;
        this.isLoading = false;
      },
      error: (err) => {
        this.loadError = 'Failed to load your leave balances. Please check the server connection.';
        this.isLoading = false;
        console.error('Leave balances load error:', err);
      }
    });

    this.essService.getLeaveRequests().subscribe({
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
    if (!this.formLeaveTypeId || this.isSaving) return;
    this.isSaving = true;
    this.actionError = '';

    this.essService.applyLeave({
      leaveTypeId: this.formLeaveTypeId,
      fromDate: this.formFromDate,
      toDate: this.formToDate,
      reason: this.formReason || undefined
    }).subscribe({
      next: () => {
        this.isSaving = false;
        this.closeForm();
        this.load();
      },
      error: (err) => {
        this.isSaving = false;
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to apply for leave.';
        console.error('Apply leave error:', err);
      }
    });
  }

  cancelRequest(request: LeaveRequest): void {
    if (!confirm('Cancel this leave request?')) return;
    this.essService.cancelLeave(request.id).subscribe({
      next: () => this.load(),
      error: (err) => {
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to cancel leave request.';
        console.error('Cancel leave error:', err);
      }
    });
  }

  isPending(request: LeaveRequest): boolean {
    return request.status === 'Pending';
  }

  trackByBalanceId(_index: number, balance: LeaveBalance): number {
    return balance.id;
  }

  trackByRequestId(_index: number, request: LeaveRequest): number {
    return request.id;
  }
}
