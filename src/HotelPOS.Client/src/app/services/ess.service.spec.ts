import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { EssService } from './ess.service';
import { environment } from '../../environments/environment';
import { Employee, EssProfileUpdateRequest } from '../models/employee.model';
import { ApplyLeaveRequest, LeaveRequest } from '../models/leave.model';

describe('EssService', () => {
  let service: EssService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiBaseUrl}/ess`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [EssService, provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(EssService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getProfile', () => {
    it('should retrieve the caller\'s own profile', () => {
      const employee = { id: 1, employeeCode: 'E001', firstName: 'Self' } as Employee;

      service.getProfile().subscribe(result => {
        expect(result).toEqual(employee);
      });

      const req = httpMock.expectOne(`${apiUrl}/profile`);
      expect(req.request.method).toBe('GET');
      req.flush(employee);
    });
  });

  describe('updateProfile', () => {
    it('should update the profile via PUT', () => {
      const request: EssProfileUpdateRequest = { phone: '9999999999' };

      service.updateProfile(request).subscribe(response => {
        expect(response).toBeNull();
      });

      const req = httpMock.expectOne(`${apiUrl}/profile`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(request);
      req.flush(null);
    });
  });

  describe('getLeaveTypes', () => {
    it('should retrieve leave types', () => {
      service.getLeaveTypes().subscribe();

      const req = httpMock.expectOne(`${apiUrl}/leave/types`);
      expect(req.request.method).toBe('GET');
      req.flush([]);
    });
  });

  describe('getLeaveBalances', () => {
    it('should omit the year param when not provided', () => {
      service.getLeaveBalances().subscribe();

      const req = httpMock.expectOne(r => r.url === `${apiUrl}/leave/balances`);
      expect(req.request.params.keys().length).toBe(0);
      req.flush([]);
    });

    it('should include the year param when provided', () => {
      service.getLeaveBalances(2025).subscribe();

      const req = httpMock.expectOne(r => r.url === `${apiUrl}/leave/balances`);
      expect(req.request.params.get('year')).toBe('2025');
      req.flush([]);
    });
  });

  describe('getLeaveRequests', () => {
    it('should include the status param when provided', () => {
      service.getLeaveRequests('Pending').subscribe();

      const req = httpMock.expectOne(r => r.url === `${apiUrl}/leave/requests`);
      expect(req.request.params.get('status')).toBe('Pending');
      req.flush([]);
    });
  });

  describe('applyLeave', () => {
    it('should submit a leave application via POST without an employeeId', () => {
      const request: Omit<ApplyLeaveRequest, 'employeeId'> = {
        leaveTypeId: 2,
        fromDate: '2025-03-01',
        toDate: '2025-03-02',
        reason: 'Personal'
      };
      const created = { id: 10, ...request } as unknown as LeaveRequest;

      service.applyLeave(request).subscribe(result => {
        expect(result).toEqual(created);
      });

      const req = httpMock.expectOne(`${apiUrl}/leave/requests`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(request);
      req.flush(created);
    });
  });

  describe('cancelLeave', () => {
    it('should post a cancel request for the given leave request id', () => {
      service.cancelLeave(10).subscribe(response => {
        expect(response).toBeNull();
      });

      const req = httpMock.expectOne(`${apiUrl}/leave/requests/10/cancel`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toBeNull();
      req.flush(null);
    });
  });

  describe('getPayslips', () => {
    it('should retrieve the caller\'s own payslips', () => {
      service.getPayslips().subscribe();

      const req = httpMock.expectOne(`${apiUrl}/payslips`);
      expect(req.request.method).toBe('GET');
      req.flush([]);
    });
  });
});
