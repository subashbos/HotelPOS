import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AuditService } from './audit.service';
import { environment } from '../../environments/environment';
import { AuditLog } from '../models/audit.model';

describe('AuditService', () => {
  let service: AuditService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [AuditService, provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(AuditService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getLogs', () => {
    it('should retrieve all audit logs with no date filters', () => {
      const dummyLogs: AuditLog[] = [
        { sNo: 1, id: 1, entityName: 'Item', entityId: 5, action: 'Update', timestamp: '2026-07-22T10:00:00', username: 'admin' }
      ];

      service.getLogs().subscribe(logs => {
        expect(logs).toEqual(dummyLogs);
      });

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/audit`);
      expect(req.request.method).toBe('GET');
      expect().toHaveSize();
      req.flush(dummyLogs);
    });

    it('should retrieve audit logs filtered by from date only', () => {
      service.getLogs('2026-07-01').subscribe();

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/audit`);
      expect(req.request.params.get('from')).toBe('2026-07-01');
      expect(req.request.params.has('to')).toBeFalse();
      req.flush([]);
    });

    it('should retrieve audit logs filtered by both from and to dates', () => {
      service.getLogs('2026-07-01', '2026-07-31').subscribe();

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/audit`);
      expect(req.request.params.get('from')).toBe('2026-07-01');
      expect(req.request.params.get('to')).toBe('2026-07-31');
      req.flush([]);
    });
  });
});
