import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { EssLeaveComponent } from './ess-leave.component';
import { EssService } from '../../../services/ess.service';
import { LeaveBalance, LeaveRequest, LeaveType } from '../../../models/leave.model';

describe('EssLeaveComponent', () => {
  let component: EssLeaveComponent;
  let fixture: ComponentFixture<EssLeaveComponent>;
  let essServiceSpy: jasmine.SpyObj<EssService>;

  const mockTypes: LeaveType[] = [
    { id: 1, code: 'CL', name: 'Casual Leave', annualQuota: 12, isPaid: true, carryForwardAllowed: false }
  ];
  const mockBalances: LeaveBalance[] = [
    { id: 1, employeeId: 1, leaveTypeId: 1, year: 2025, entitledDays: 12, usedDays: 2, pendingDays: 0, availableDays: 10 }
  ];
  const mockRequests: LeaveRequest[] = [
    { id: 1, employeeId: 1, leaveTypeId: 1, fromDate: '2025-01-01', toDate: '2025-01-02', totalDays: 2, status: 'Pending', appliedOn: '2025-01-01T00:00:00Z' }
  ];

  beforeEach(async () => {
    essServiceSpy = jasmine.createSpyObj('EssService', [
      'getLeaveTypes', 'getLeaveBalances', 'getLeaveRequests', 'applyLeave', 'cancelLeave'
    ]);
    essServiceSpy.getLeaveTypes.and.returnValue(of(mockTypes));
    essServiceSpy.getLeaveBalances.and.returnValue(of(mockBalances));
    essServiceSpy.getLeaveRequests.and.returnValue(of(mockRequests));

    await TestBed.configureTestingModule({
      declarations: [EssLeaveComponent],
      imports: [FormsModule],
      providers: [{ provide: EssService, useValue: essServiceSpy }]
    }).compileComponents();

    fixture = TestBed.createComponent(EssLeaveComponent);
    component = fixture.componentInstance;
  });

  it('should create and load types, balances, and requests', () => {
    fixture.detectChanges();

    expect(component).toBeTruthy();
    expect(component.leaveTypes).toEqual(mockTypes);
    expect(component.balances).toEqual(mockBalances);
    expect(component.requests.length).toBe(1);
    expect(component.isLoading).toBeFalse();
  });

  it('should handle balances load error', () => {
    spyOn(console, 'error');
    essServiceSpy.getLeaveBalances.and.returnValue(throwError(() => new Error('boom')));

    fixture.detectChanges();

    expect(component.loadError).toContain('Failed to load your leave balances');
  });

  it('should log leave-types load errors without surfacing loadError', () => {
    spyOn(console, 'error');
    essServiceSpy.getLeaveTypes.and.returnValue(throwError(() => new Error('boom')));

    fixture.detectChanges();

    expect(console.error).toHaveBeenCalled();
    expect(component.loadError).toBe('');
  });

  it('should open the apply form defaulted to the first leave type', () => {
    fixture.detectChanges();

    component.openApplyForm();

    expect(component.showForm).toBeTrue();
    expect(component.formLeaveTypeId).toBe(1);
  });

  it('should close the apply form', () => {
    fixture.detectChanges();
    component.showForm = true;

    component.closeForm();

    expect(component.showForm).toBeFalse();
  });

  it('should not apply leave without a selected leave type', () => {
    fixture.detectChanges();
    component.formLeaveTypeId = null;

    component.applyLeave();

    expect(essServiceSpy.applyLeave).not.toHaveBeenCalled();
  });

  it('should apply leave and reload on success', () => {
    fixture.detectChanges();
    component.formLeaveTypeId = 1;
    essServiceSpy.applyLeave.and.returnValue(of(mockRequests[0]));

    component.applyLeave();

    expect(essServiceSpy.applyLeave).toHaveBeenCalled();
    expect(component.showForm).toBeFalse();
    expect(component.isSaving).toBeFalse();
  });

  it('should surface apply errors', () => {
    spyOn(console, 'error');
    fixture.detectChanges();
    component.formLeaveTypeId = 1;
    essServiceSpy.applyLeave.and.returnValue(throwError(() => ({ error: { message: 'Apply failed' } })));

    component.applyLeave();

    expect(component.actionError).toBe('Apply failed');
    expect(component.isSaving).toBeFalse();
  });

  it('should cancel a request when confirmed', () => {
    fixture.detectChanges();
    spyOn(window, 'confirm').and.returnValue(true);
    essServiceSpy.cancelLeave.and.returnValue(of(void 0));

    component.cancelRequest(mockRequests[0]);

    expect(essServiceSpy.cancelLeave).toHaveBeenCalledWith(1);
  });

  it('should not cancel a request when not confirmed', () => {
    fixture.detectChanges();
    spyOn(window, 'confirm').and.returnValue(false);

    component.cancelRequest(mockRequests[0]);

    expect(essServiceSpy.cancelLeave).not.toHaveBeenCalled();
  });

  it('should surface cancel errors', () => {
    spyOn(console, 'error');
    fixture.detectChanges();
    spyOn(window, 'confirm').and.returnValue(true);
    essServiceSpy.cancelLeave.and.returnValue(throwError(() => ({ error: { message: 'Cancel failed' } })));

    component.cancelRequest(mockRequests[0]);

    expect(component.actionError).toBe('Cancel failed');
  });

  it('should report pending status correctly', () => {
    expect(component.isPending(mockRequests[0])).toBeTrue();
    expect(component.isPending({ ...mockRequests[0], status: 'Approved' })).toBeFalse();
  });

  it('should track balances and requests by id', () => {
    expect(component.trackByBalanceId(0, mockBalances[0])).toBe(1);
    expect(component.trackByRequestId(0, mockRequests[0])).toBe(1);
  });
});
