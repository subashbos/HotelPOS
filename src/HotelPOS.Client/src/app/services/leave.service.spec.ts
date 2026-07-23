import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { LeaveService } from './leave.service';
import { environment } from '../../environments/environment';
import { ApplyLeaveRequest, LeaveBalance, LeaveRequest, LeaveType } from '../models/leave.model';

describe('LeaveService', () => {
  let service: LeaveService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [LeaveService, provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(LeaveService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getLeaveTypes', () => {
    it('should retrieve all leave types', () => {
      const dummyTypes: LeaveType[] = [
        { id: 1, code: 'CL', name: 'Casual Leave', annualQuota: 12, isPaid: true, carryForwardAllowed: false }
      ];

      service.getLeaveTypes().subscribe(types => {
        expect(types).toEqual(dummyTypes);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/leave/types`);
      expect(req.request.method).toBe('GET');
      req.flush(dummyTypes);
    });
  });

  describe('getBalances', () => {
    it('should retrieve leave balances for an employee without year param', () => {
      const dummyBalances: LeaveBalance[] = [
        { id: 1, employeeId: 5, leaveTypeId: 1, year: 2026, entitledDays: 12, usedDays: 2, pendingDays: 0, availableDays: 10 }
      ];

      service.getBalances(5).subscribe(balances => {
        expect(balances).toEqual(dummyBalances);
      });

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/leave/balances/5`);
      expect(req.request.method).toBe('GET');
      expect(req.request.params.keys()).toHaveSize(0);
      req.flush(dummyBalances);
    });

    it('should retrieve leave balances for an employee with a year param', () => {
      const dummyBalances: LeaveBalance[] = [];

      service.getBalances(5, 2025).subscribe(balances => {
        expect(balances).toEqual(dummyBalances);
      });

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/leave/balances/5`);
      expect(req.request.params.get('year')).toBe('2025');
      req.flush(dummyBalances);
    });
  });

  describe('getRequests', () => {
    it('should retrieve all leave requests with no filters', () => {
      const dummyRequests: LeaveRequest[] = [];

      service.getRequests().subscribe(requests => {
        expect(requests).toEqual(dummyRequests);
      });

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/leave/requests`);
      expect(req.request.params.keys()).toHaveSize(0);
      req.flush(dummyRequests);
    });

    it('should retrieve leave requests filtered by employeeId', () => {
      service.getRequests(5).subscribe();

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/leave/requests`);
      expect(req.request.params.get('employeeId')).toBe('5');
      expect(req.request.params.has('status')).toBeFalse();
      req.flush([]);
    });

    it('should retrieve leave requests filtered by status', () => {
      service.getRequests(undefined, 'Pending').subscribe();

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/leave/requests`);
      expect(req.request.params.has('employeeId')).toBeFalse();
      expect(req.request.params.get('status')).toBe('Pending');
      req.flush([]);
    });

    it('should retrieve leave requests filtered by both employeeId and status', () => {
      service.getRequests(5, 'Approved').subscribe();

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/leave/requests`);
      expect(req.request.params.get('employeeId')).toBe('5');
      expect(req.request.params.get('status')).toBe('Approved');
      req.flush([]);
    });
  });

  describe('applyLeave', () => {
    it('should submit a leave application via POST', () => {
      const request: ApplyLeaveRequest = {
        employeeId: 5,
        leaveTypeId: 1,
        fromDate: '2026-08-01',
        toDate: '2026-08-03',
        reason: 'Family event'
      };
      const createdRequest: LeaveRequest = {
        id: 1,
        employeeId: 5,
        leaveTypeId: 1,
        fromDate: '2026-08-01',
        toDate: '2026-08-03',
        totalDays: 3,
        status: 'Pending',
        appliedOn: '2026-07-22'
      };

      service.applyLeave(request).subscribe(response => {
        expect(response).toEqual(createdRequest);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/leave/requests`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(request);
      req.flush(createdRequest);
    });
  });

  describe('approveLeave', () => {
    it('should approve a leave request with approverEmployeeId param', () => {
      service.approveLeave(1, 10).subscribe(response => {
        expect(response).toBeNull();
      });

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/leave/requests/1/approve`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toBeNull();
      expect(req.request.params.get('approverEmployeeId')).toBe('10');
      req.flush(null);
    });
  });

  describe('rejectLeave', () => {
    it('should reject a leave request with reason and approverEmployeeId param', () => {
      service.rejectLeave(1, 10, 'Insufficient balance').subscribe(response => {
        expect(response).toBeNull();
      });

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/leave/requests/1/reject`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ reason: 'Insufficient balance' });
      expect(req.request.params.get('approverEmployeeId')).toBe('10');
      req.flush(null);
    });
  });
});
