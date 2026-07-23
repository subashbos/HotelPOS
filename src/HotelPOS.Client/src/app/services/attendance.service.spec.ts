import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AttendanceService } from './attendance.service';
import { environment } from '../../environments/environment';
import { Attendance, MarkAttendanceRequest } from '../models/attendance.model';

describe('AttendanceService', () => {
  let service: AttendanceService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [AttendanceService, provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(AttendanceService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getAttendance', () => {
    it('should retrieve attendance records with employeeId, from and to params', () => {
      const dummyAttendance: Attendance[] = [
        { id: 1, employeeId: 5, date: '2026-07-01', status: 'Present', workedHours: 8 },
        { id: 2, employeeId: 5, date: '2026-07-02', status: 'Absent', workedHours: 0 }
      ];

      service.getAttendance(5, '2026-07-01', '2026-07-31').subscribe(records => {
        expect(records).toHaveSize(2);
        expect(records).toEqual(dummyAttendance);
      });

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/attendance`);
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('employeeId')).toBe('5');
      expect(req.request.params.get('from')).toBe('2026-07-01');
      expect(req.request.params.get('to')).toBe('2026-07-31');
      req.flush(dummyAttendance);
    });
  });

  describe('markAttendance', () => {
    it('should mark attendance via POST', () => {
      const request: MarkAttendanceRequest = {
        id: 0,
        employeeId: 5,
        date: '2026-07-01',
        status: 'Present'
      };
      const createdAttendance: Attendance = {
        id: 1,
        employeeId: 5,
        date: '2026-07-01',
        status: 'Present',
        workedHours: 8
      };

      service.markAttendance(request).subscribe(attendance => {
        expect(attendance).toEqual(createdAttendance);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/attendance`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(request);
      req.flush(createdAttendance);
    });
  });

  describe('deleteAttendance', () => {
    it('should delete attendance via DELETE', () => {
      service.deleteAttendance(1).subscribe(response => {
        expect(response).toBeNull();
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/attendance/1`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });
  });
});
