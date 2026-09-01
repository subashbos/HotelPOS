import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ReportService } from './report.service';
import { environment } from '../../environments/environment';
import {
  GstR1Report, ItemMarginRow, ItemReportRow, LedgerReportRow, LowStockAlert, MonthlySalesChart, MonthlyTrend,
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
      expect(req.request.params.keys()).toHaveSize(0);
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

  describe('exportSalesReport', () => {
    it('should request the sales report export as a blob', () => {
      const dummyBlob = new Blob(['data']);

      service.exportSalesReport('2026-07-01', '2026-07-31').subscribe(blob => {
        expect(blob).toEqual(dummyBlob);
      });

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/reports/sales/export`);
      expect(req.request.responseType).toBe('blob');
      req.flush(dummyBlob);
    });
  });

  describe('exportItemReport', () => {
    it('should request the item report export as a blob', () => {
      const dummyBlob = new Blob(['data']);

      service.exportItemReport('2026-07-01', '2026-07-31').subscribe(blob => {
        expect(blob).toEqual(dummyBlob);
      });

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/reports/items/export`);
      expect(req.request.responseType).toBe('blob');
      req.flush(dummyBlob);
    });
  });

  describe('exportPurchaseReport', () => {
    it('should request the purchase report export as a blob', () => {
      const dummyBlob = new Blob(['data']);

      service.exportPurchaseReport('2026-07-01', '2026-07-31').subscribe(blob => {
        expect(blob).toEqual(dummyBlob);
      });

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/reports/purchases/export`);
      expect(req.request.responseType).toBe('blob');
      req.flush(dummyBlob);
    });

    it('should include supplier/item/payment/invoice filters when provided', () => {
      service.exportPurchaseReport(undefined, undefined, {
        supplierId: 7, itemName: 'Rice', paymentType: 'Credit', invoiceNo: 'INV-1'
      }).subscribe();

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/reports/purchases/export`);
      expect(req.request.params.get('supplierId')).toBe('7');
      expect(req.request.params.get('itemName')).toBe('Rice');
      expect(req.request.params.get('paymentType')).toBe('Credit');
      expect(req.request.params.get('invoiceNo')).toBe('INV-1');
      req.flush(new Blob(['data']));
    });
  });

  describe('exportLedgerReport', () => {
    it('should request the ledger report export as a blob', () => {
      const dummyBlob = new Blob(['data']);

      service.exportLedgerReport('2026-07-01', '2026-07-31').subscribe(blob => {
        expect(blob).toEqual(dummyBlob);
      });

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/reports/ledger/export`);
      expect(req.request.responseType).toBe('blob');
      req.flush(dummyBlob);
    });
  });

  describe('getLedgerReport', () => {
    it('should retrieve the ledger report with required from and to params', () => {
      const dummyRows: LedgerReportRow[] = [
        { sNo: 1, date: '2026-07-01', orderCount: 10, grossRevenue: 1000, gstAmount: 50, netIncome: 950 }
      ];

      service.getLedgerReport('2026-07-01', '2026-07-31').subscribe(rows => {
        expect(rows).toEqual(dummyRows);
      });

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/reports/ledger`);
      expect(req.request.params.get('from')).toBe('2026-07-01');
      expect(req.request.params.get('to')).toBe('2026-07-31');
      req.flush(dummyRows);
    });
  });

  describe('getGstR1Report', () => {
    it('should retrieve GSTR-1 report with required from and to params', () => {
      const dummyReport: GstR1Report = {
        b2BRows: [
          { sNo: 1, gstin: '33AQZPS2365E1ZE', invoiceNumber: 'INV24', date: '2026-07-01', invoiceValue: 6615,
            pos: '33', reverseCharge: 'N', invoiceType: 'R', customerName: 'Anu Labs', taxableValue: 6300,
            itemTotal: 6615, rate: 5, cgst: 157.5, sgst: 157.5, igst: 0 }
        ],
        b2cSummary: [
          { rate: 5, invoiceCount: 2, taxableValue: 7300, cgst: 182.5, sgst: 182.5, igst: 0, totalTax: 365, totalValue: 7665 }
        ],
        hsnSummary: [
          { hsnCode: '2106', description: 'Chicken Biriyani', uqc: 'Plate', totalQuantity: 3, taxableValue: 300,
            rate: 5, cgst: 7.5, sgst: 7.5, igst: 0, totalTax: 15, totalValue: 315 }
        ]
      };

      service.getGstR1Report('2026-07-01', '2026-07-31').subscribe(report => {
        expect(report).toEqual(dummyReport);
      });

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/reports/gstr1`);
      expect(req.request.params.get('from')).toBe('2026-07-01');
      expect(req.request.params.get('to')).toBe('2026-07-31');
      req.flush(dummyReport);
    });
  });

  describe('exportGstR1Report', () => {
    it('should request the GSTR-1 report export as a blob', () => {
      const dummyBlob = new Blob(['data']);

      service.exportGstR1Report('2026-07-01', '2026-07-31').subscribe(blob => {
        expect(blob).toEqual(dummyBlob);
      });

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/reports/gstr1/export`);
      expect(req.request.responseType).toBe('blob');
      req.flush(dummyBlob);
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

    it('should include supplier/item/payment/invoice filters when provided', () => {
      service.getPurchaseReport(1, 20, undefined, undefined, {
        supplierId: 7, itemName: 'Rice', paymentType: 'Credit', invoiceNo: 'INV-1'
      }).subscribe();

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/reports/purchases`);
      expect(req.request.params.get('supplierId')).toBe('7');
      expect(req.request.params.get('itemName')).toBe('Rice');
      expect(req.request.params.get('paymentType')).toBe('Credit');
      expect(req.request.params.get('invoiceNo')).toBe('INV-1');
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
