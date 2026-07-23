import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ReportService } from './report.service';
import { environment } from '../../environments/environment';
import {
  GstReportRow, ItemMarginRow, ItemReportRow, LowStockAlert, MonthlySalesChart, MonthlyTrend,
  PagedPurchaseReport, ProfitMarginSummary, SalesReport, WastageSummary
} from '../models/report.model';

describe('ReportService', () => {
  let service: ReportService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ReportService, provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(ReportService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getSalesReport', () => {
    it('should retrieve sales report with no date filters', () => {
      const dummyReport: SalesReport = {
        totalRevenue: 10000, totalOrders: 50, averageOrderValue: 200, mostPopularItem: 'Burger',
        salesByTable: [], recentOrders: [], salesByCategory: [], salesByPaymentMode: []
      };

      service.getSalesReport().subscribe(report => {
        expect(report).toEqual(dummyReport);
      });

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/reports/sales`);
      expect(req.request.params.keys().length).toBe(0);
      req.flush(dummyReport);
    });

    it('should retrieve sales report with from/to date filters', () => {
      service.getSalesReport('2026-07-01', '2026-07-31').subscribe();

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/reports/sales`);
      expect(req.request.params.get('from')).toBe('2026-07-01');
      expect(req.request.params.get('to')).toBe('2026-07-31');
      req.flush({} as SalesReport);
    });
  });

  describe('getItemReport', () => {
    it('should retrieve item report rows', () => {
      const dummyRows: ItemReportRow[] = [
        { sNo: 1, itemName: 'Burger', totalQtySold: 100, totalRevenue: 5000, unitPrice: 50 }
      ];

      service.getItemReport('2026-07-01', '2026-07-31').subscribe(rows => {
        expect(rows).toEqual(dummyRows);
      });

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/reports/items`);
      expect(req.request.params.get('from')).toBe('2026-07-01');
      expect(req.request.params.get('to')).toBe('2026-07-31');
      req.flush(dummyRows);
    });
  });

  describe('getGstReport', () => {
    it('should retrieve GST report with required from and to params', () => {
      const dummyRows: GstReportRow[] = [
        { sNo: 1, date: '2026-07-01', orderCount: 10, grossRevenue: 1000, gstAmount: 50, netIncome: 950 }
      ];

      service.getGstReport('2026-07-01', '2026-07-31').subscribe(rows => {
        expect(rows).toEqual(dummyRows);
      });

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/reports/gst`);
      expect(req.request.params.get('from')).toBe('2026-07-01');
      expect(req.request.params.get('to')).toBe('2026-07-31');
      req.flush(dummyRows);
    });
  });

  describe('getMonthlyChart', () => {
    it('should retrieve monthly sales chart data', () => {
      const dummyChart: MonthlySalesChart[] = [{ monthName: 'Jul', revenue: 10000 }];

      service.getMonthlyChart().subscribe(chart => {
        expect(chart).toEqual(dummyChart);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/reports/monthly-chart`);
      expect(req.request.method).toBe('GET');
      req.flush(dummyChart);
    });
  });

  describe('getPurchaseReport', () => {
    it('should retrieve paged purchase report with page/pageSize and no date filters', () => {
      const dummyReport: PagedPurchaseReport = {
        items: [], totalCount: 0, totalPurchases: 0, totalTax: 0, totalDiscount: 0, totalQty: 0
      };

      service.getPurchaseReport(1, 20).subscribe(report => {
        expect(report).toEqual(dummyReport);
      });

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/reports/purchases`);
      expect(req.request.params.get('page')).toBe('1');
      expect(req.request.params.get('pageSize')).toBe('20');
      expect(req.request.params.has('from')).toBeFalse();
      req.flush(dummyReport);
    });

    it('should retrieve paged purchase report with date filters', () => {
      service.getPurchaseReport(2, 10, '2026-07-01', '2026-07-31').subscribe();

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/reports/purchases`);
      expect(req.request.params.get('page')).toBe('2');
      expect(req.request.params.get('pageSize')).toBe('10');
      expect(req.request.params.get('from')).toBe('2026-07-01');
      expect(req.request.params.get('to')).toBe('2026-07-31');
      req.flush({} as PagedPurchaseReport);
    });
  });

  describe('getMarginSummary', () => {
    it('should retrieve profit margin summary', () => {
      const dummySummary: ProfitMarginSummary = {
        totalRevenue: 10000, totalCogs: 4000, grossProfit: 6000, totalExpenses: 2000,
        netProfit: 4000, marginPercentage: 60, foodCostPercentage: 40
      };

      service.getMarginSummary().subscribe(summary => {
        expect(summary).toEqual(dummySummary);
      });

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/reports/margins/summary`);
      req.flush(dummySummary);
    });
  });

  describe('getItemMargins', () => {
    it('should retrieve item margin rows', () => {
      const dummyRows: ItemMarginRow[] = [
        {
          sNo: 1, itemName: 'Burger', categoryName: 'Food', quantitySold: 100, unitPrice: 50,
          costPrice: 20, totalRevenue: 5000, totalCogs: 2000, profit: 3000, marginPercentage: 60,
          recommendation: 'Keep'
        }
      ];

      service.getItemMargins().subscribe(rows => {
        expect(rows).toEqual(dummyRows);
      });

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/reports/margins/items`);
      req.flush(dummyRows);
    });
  });

  describe('getWastageSummary', () => {
    it('should retrieve wastage summary', () => {
      const dummySummary: WastageSummary = {
        totalWastageCost: 500, totalWastageQty: 20, reasonsBreakdown: [], recentWastage: []
      };

      service.getWastageSummary().subscribe(summary => {
        expect(summary).toEqual(dummySummary);
      });

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/reports/wastage`);
      req.flush(dummySummary);
    });
  });

  describe('getLowStockAlerts', () => {
    it('should retrieve low stock alerts', () => {
      const dummyAlerts: LowStockAlert[] = [
        { sNo: 1, itemId: 1, itemName: 'Flour', currentStock: 5, minThreshold: 10, dailyConsumptionRate: 2, daysRemaining: 2.5, alertLevel: 'Critical' }
      ];

      service.getLowStockAlerts().subscribe(alerts => {
        expect(alerts).toEqual(dummyAlerts);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/reports/low-stock`);
      expect(req.request.method).toBe('GET');
      req.flush(dummyAlerts);
    });
  });

  describe('getMonthlyTrend', () => {
    it('should retrieve monthly trend data', () => {
      const dummyTrend: MonthlyTrend[] = [
        { monthName: 'Jul', revenue: 10000, grossProfit: 6000, netProfit: 4000 }
      ];

      service.getMonthlyTrend().subscribe(trend => {
        expect(trend).toEqual(dummyTrend);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/reports/monthly-trend`);
      expect(req.request.method).toBe('GET');
      req.flush(dummyTrend);
    });
  });
});
