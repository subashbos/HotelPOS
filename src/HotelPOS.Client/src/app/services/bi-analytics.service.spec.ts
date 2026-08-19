import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { BiAnalyticsService } from './bi-analytics.service';
import { environment } from '../../environments/environment';

describe('BiAnalyticsService', () => {
  let service: BiAnalyticsService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiBaseUrl}/reports/bi-analytics`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [BiAnalyticsService, provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(BiAnalyticsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getBiAnalytics', () => {
    it('should request without date params when none are given', () => {
      const response = {
        kpis: { totalRevenue: 100000, netProfit: 20000, foodCostPercentage: 30, totalWastageCost: 500, cogs: 40000, totalExpenses: 15000 },
        monthlyTrends: [{ monthName: 'Jan', revenue: 10000, profit: 2000 }]
      };

      service.getBiAnalytics().subscribe(result => {
        expect(result).toEqual(response);
      });

      const req = httpMock.expectOne(r => r.url === apiUrl);
      expect(req.request.method).toBe('GET');
      expect(req.request.params.keys().length).toBe(0);
      req.flush(response);
    });

    it('should include fromDate and toDate as query params when provided', () => {
      const response = {
        kpis: { totalRevenue: 0, netProfit: 0, foodCostPercentage: 0, totalWastageCost: 0, cogs: 0, totalExpenses: 0 },
        monthlyTrends: []
      };

      service.getBiAnalytics('2025-01-01', '2025-01-31').subscribe(result => {
        expect(result).toEqual(response);
      });

      const req = httpMock.expectOne(r => r.url === apiUrl);
      expect(req.request.params.get('fromDate')).toBe('2025-01-01');
      expect(req.request.params.get('toDate')).toBe('2025-01-31');
      req.flush(response);
    });
  });
});
