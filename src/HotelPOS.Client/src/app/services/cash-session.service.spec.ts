import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { CashSessionService } from './cash-session.service';
import { environment } from '../../environments/environment';
import { CashSession } from '../models/cash-session.model';

describe('CashSessionService', () => {
  let service: CashSessionService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [CashSessionService, provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(CashSessionService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getCurrentSession', () => {
    it('should retrieve the current open session', () => {
      const dummySession: CashSession = {
        sNo: 1, id: 1, openedAt: '2026-07-22T09:00:00', openingBalance: 1000, openedBy: 'admin', status: 'Open'
      };

      service.getCurrentSession().subscribe(session => {
        expect(session).toEqual(dummySession);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/cashsessions/current`);
      expect(req.request.method).toBe('GET');
      req.flush(dummySession);
    });

    it('should return null when no session exists (404)', () => {
      service.getCurrentSession().subscribe(session => {
        expect(session).toBeNull();
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/cashsessions/current`);
      req.flush('Not found', { status: 404, statusText: 'Not Found' });
    });

    it('should propagate non-404 errors', () => {
      service.getCurrentSession().subscribe({
        next: () => fail('expected an error'),
        error: (err) => expect(err.status).toBe(500)
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/cashsessions/current`);
      req.flush('Server error', { status: 500, statusText: 'Internal Server Error' });
    });
  });

  describe('getHistory', () => {
    it('should retrieve session history with default count', () => {
      const dummyHistory: CashSession[] = [
        { sNo: 1, id: 1, openedAt: '2026-07-21T09:00:00', closedAt: '2026-07-21T21:00:00', openingBalance: 1000, closingBalance: 5000, openedBy: 'admin', closedBy: 'admin', status: 'Closed' }
      ];

      service.getHistory().subscribe(history => {
        expect(history).toEqual(dummyHistory);
      });

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/cashsessions/history`);
      expect(req.request.params.get('count')).toBe('30');
      req.flush(dummyHistory);
    });

    it('should retrieve session history with custom count', () => {
      service.getHistory(10).subscribe();

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/cashsessions/history`);
      expect(req.request.params.get('count')).toBe('10');
      req.flush([]);
    });
  });

  describe('getCurrentSessionSalesTotal', () => {
    it('should retrieve the current session sales total', () => {
      service.getCurrentSessionSalesTotal().subscribe(total => {
        expect(total).toBe(15000);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/cashsessions/current/sales-total`);
      expect(req.request.method).toBe('GET');
      req.flush(15000);
    });
  });

  describe('openSession', () => {
    it('should open a new session with an opening balance via POST', () => {
      service.openSession(1000).subscribe(id => {
        expect(id).toBe(5);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/cashsessions/open`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ openingBalance: 1000 });
      req.flush(5);
    });
  });

  describe('closeSession', () => {
    it('should close a session with actual cash and notes via POST', () => {
      service.closeSession(4800, 'Short by 200').subscribe(response => {
        expect(response).toBeNull();
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/cashsessions/close`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ actualCash: 4800, notes: 'Short by 200' });
      req.flush(null);
    });

    it('should close a session without notes', () => {
      service.closeSession(5000).subscribe();

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/cashsessions/close`);
      expect(req.request.body).toEqual({ actualCash: 5000, notes: undefined });
      req.flush({});
    });
  });
});
