import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { LeaveComponent } from './leave.component';
import { LeaveService } from '../../../services/leave.service';
import { EmployeeService } from '../../../services/employee.service';
import { LeaveBalance, LeaveRequest } from '../../../models/leave.model';

describe('LeaveComponent', () => {
  let component: LeaveComponent;
  let fixture: ComponentFixture<LeaveComponent>;
  let leaveServiceSpy: jasmine.SpyObj<LeaveService>;
  let employeeServiceSpy: jasmine.SpyObj<EmployeeService>;

  const mockRequest: LeaveRequest = {
    id: 1,
    employeeId: 1,
    employeeName: 'John Doe',
    leaveTypeId: 1,
    leaveTypeName: 'Casual Leave',
    fromDate: '2026-02-01',
    toDate: '2026-02-02',
    totalDays: 2,
    status: 'Pending',
    appliedOn: '2026-01-20'
  };

  const mockBalance: LeaveBalance = {
    id: 10,
    employeeId: 1,
    leaveTypeId: 1,
    leaveTypeName: 'Casual Leave',
    year: 2026,
    entitledDays: 12,
    usedDays: 2,
    pendingDays: 0,
    availableDays: 10
  };

  beforeEach(async () => {
    leaveServiceSpy = jasmine.createSpyObj('LeaveService', [
      'getLeaveTypes',
      'getBalances',
      'getRequests',
      'applyLeave',
      'approveLeave',
      'rejectLeave'
    ]);
    employeeServiceSpy = jasmine.createSpyObj('EmployeeService', ['getEmployees']);

    leaveServiceSpy.getLeaveTypes.and.returnValue(of([{ id: 1, name: 'Casual', code: 'CL', annualQuota: 12, isPaid: true, carryForwardAllowed: false }]));
    leaveServiceSpy.getBalances.and.returnValue(of([mockBalance]));
    leaveServiceSpy.getRequests.and.returnValue(of([mockRequest]));
    employeeServiceSpy.getEmployees.and.returnValue(of([{ id: 1, firstName: 'John', lastName: 'Doe', employeeCode: 'EMP001' } as any]));

    await TestBed.configureTestingModule({
      declarations: [LeaveComponent],
      imports: [FormsModule],
      providers: [
        { provide: LeaveService, useValue: leaveServiceSpy },
        { provide: EmployeeService, useValue: employeeServiceSpy }
      ]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(LeaveComponent);
    component = fixture.componentInstance;
  });

  it('should create component and load leave data', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
    expect(employeeServiceSpy.getEmployees).toHaveBeenCalled();
    expect(leaveServiceSpy.getBalances).toHaveBeenCalledWith(1);
    expect(component.balances).toHaveSize(1);
    expect(component.requests).toHaveSize(1);
  });

  it('should open apply form and close form', () => {
    fixture.detectChanges();
    component.openApplyForm();
    expect(component.showForm).toBeTrue();
    expect(component.formLeaveTypeId).toBe(1);

    component.closeForm();
    expect(component.showForm).toBeFalse();
  });

  it('should apply leave successfully', () => {
    leaveServiceSpy.applyLeave.and.returnValue(of({ id: 2 } as any));
    fixture.detectChanges();
    component.openApplyForm();
    component.formReason = 'Personal work';

    component.applyLeave();

    expect(leaveServiceSpy.applyLeave).toHaveBeenCalled();
    expect(component.showForm).toBeFalse();
  });

  it('should validate approver for approve/reject', () => {
    fixture.detectChanges();
    component.approve(mockRequest);
    expect(component.actionError).toBe('Select an approving employee first.');

    component.approverEmployeeId = 2;
    leaveServiceSpy.approveLeave.and.returnValue(of(void 0));
    component.approve(mockRequest);
    expect(leaveServiceSpy.approveLeave).toHaveBeenCalledWith(1, 2);
  });

  it('should handle reject leave flow', () => {
    fixture.detectChanges();
    component.startReject(mockRequest);
    expect(component.rejectingRequestId).toBe(1);

    component.cancelReject();
    expect(component.rejectingRequestId).toBeNull();

    component.startReject(mockRequest);
    component.approverEmployeeId = 2;
    component.rejectReason = 'Not allowed';
    leaveServiceSpy.rejectLeave.and.returnValue(of(void 0));

    component.confirmReject(mockRequest);
    expect(leaveServiceSpy.rejectLeave).toHaveBeenCalledWith(1, 2, 'Not allowed');
  });

  it('should track balance and request by id', () => {
    expect(component.trackByBalanceId(0, mockBalance)).toBe(10);
    expect(component.trackByRequestId(0, mockRequest)).toBe(1);
  });
});
