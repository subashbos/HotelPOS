export interface LeaveType {
  id: number;
  code: string;
  name: string;
  annualQuota: number;
  isPaid: boolean;
  carryForwardAllowed: boolean;
}

export interface LeaveBalance {
  id: number;
  employeeId: number;
  leaveTypeId: number;
  leaveTypeName?: string;
  year: number;
  entitledDays: number;
  usedDays: number;
  pendingDays: number;
  availableDays: number;
}

export interface LeaveRequest {
  id: number;
  employeeId: number;
  employeeName?: string;
  leaveTypeId: number;
  leaveTypeName?: string;
  fromDate: string;
  toDate: string;
  totalDays: number;
  reason?: string;
  status: string;
  appliedOn: string;
  approvedByEmployeeId?: number;
  actionedOn?: string;
  rejectionReason?: string;
}

export interface ApplyLeaveRequest {
  employeeId: number;
  leaveTypeId: number;
  fromDate: string;
  toDate: string;
  reason?: string;
}
