import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { BiAnalyticsComponent } from './bi-analytics.component';
import { BiAnalyticsService } from '../../../services/bi-analytics.service';
import { ReportService } from '../../../services/report.service';
import { ItemMarginRow } from '../../../models/report.model';

describe('BiAnalyticsComponent', () => {
  let component: BiAnalyticsComponent;
  let fixture: ComponentFixture<BiAnalyticsComponent>;
  let biServiceSpy: jasmine.SpyObj<BiAnalyticsService>;
  let reportServiceSpy: jasmine.SpyObj<ReportService>;

  const mockMarginRows: ItemMarginRow[] = [
    { sNo: 1, itemName: 'Cold Coffee', categoryName: 'Beverages', quantitySold: 10, unitPrice: 120, costPrice: 22, totalRevenue: 1200, totalCogs: 220, profit: 980, marginPercentage: 81.6, recommendation: '' },
    { sNo: 2, itemName: 'Veg Fried Rice', categoryName: 'Main Course', quantitySold: 5, unitPrice: 180, costPrice: 45, totalRevenue: 900, totalCogs: 225, profit: 675, marginPercentage: 75.0, recommendation: '' }
  ];

  beforeEach(async () => {
    biServiceSpy = jasmine.createSpyObj('BiAnalyticsService', ['getBiAnalytics']);
    reportServiceSpy = jasmine.createSpyObj('ReportService', [
      'getShiftClosureReport', 'getVoidDiscountAudit', 'getStaffPerformanceReport',
      'getStockValuationReport', 'getProfitAndLossReport', 'getItemMargins'
    ]);

    biServiceSpy.getBiAnalytics.and.returnValue(of({
      kpis: { totalRevenue: 1, netProfit: 1, foodCostPercentage: 1, totalWastageCost: 1, cogs: 1, totalExpenses: 1 },
      monthlyTrends: [{ monthName: 'Jul', revenue: 1000, profit: 200 }]
    }));
    reportServiceSpy.getShiftClosureReport.and.returnValue(of({} as any));
    reportServiceSpy.getVoidDiscountAudit.and.returnValue(of([]));
    reportServiceSpy.getStaffPerformanceReport.and.returnValue(of([]));
    reportServiceSpy.getStockValuationReport.and.returnValue(of({} as any));
    reportServiceSpy.getProfitAndLossReport.and.returnValue(of({} as any));
    reportServiceSpy.getItemMargins.and.returnValue(of(mockMarginRows));

    await TestBed.configureTestingModule({
      declarations: [BiAnalyticsComponent],
      imports: [FormsModule],
      providers: [
        { provide: BiAnalyticsService, useValue: biServiceSpy },
        { provide: ReportService, useValue: reportServiceSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(BiAnalyticsComponent);
    component = fixture.componentInstance;
  });

  it('should create, default the date range to the current month, and load data', () => {
    fixture.detectChanges();

    expect(component).toBeTruthy();
    expect(component.fromDate).toBeTruthy();
    expect(component.toDate).toBeTruthy();
    expect(biServiceSpy.getBiAnalytics).toHaveBeenCalledWith(component.fromDate, component.toDate);
    expect(component.isLoading).toBeFalse();
    expect(component.monthlyTrends[0].monthName).toBe('Jul');
  });

  it('should start with zeroed KPIs and no fabricated data before load', () => {
    expect(component.kpis.totalRevenue).toBe(0);
    expect(component.monthlyTrends).toEqual([]);
    expect(component.topMarginItems).toEqual([]);
  });

  it('should load top margin items from the item margins report, sorted descending', () => {
    fixture.detectChanges();

    expect(reportServiceSpy.getItemMargins).toHaveBeenCalledWith(component.fromDate, component.toDate);
    expect(component.topMarginItems.map(i => i.itemName)).toEqual(['Cold Coffee', 'Veg Fried Rice']);
  });

  it('should surface an error and not fall back to fabricated data when the overview KPI load fails', () => {
    biServiceSpy.getBiAnalytics.and.returnValue(throwError(() => new Error('boom')));

    fixture.detectChanges();

    expect(component.kpis.totalRevenue).toBe(0);
    expect(component.errorMessage).toContain('Failed to load the overview KPIs');
  });

  it('should clear top margin items and not throw when the item margins load fails', () => {
    reportServiceSpy.getItemMargins.and.returnValue(throwError(() => new Error('boom')));

    fixture.detectChanges();

    expect(component.topMarginItems).toEqual([]);
  });

  it('should switch tabs', () => {
    fixture.detectChanges();

    component.setTab('pnl');

    expect(component.activeTab).toBe('pnl');
  });

  it('should reload data on refresh', () => {
    fixture.detectChanges();
    biServiceSpy.getBiAnalytics.calls.reset();

    component.refresh();

    expect(biServiceSpy.getBiAnalytics).toHaveBeenCalled();
  });

  it('should compute max revenue with a floor of 500000', () => {
    fixture.detectChanges();
    component.monthlyTrends = [{ monthName: 'A', revenue: 100, profit: 0 }];

    expect(component.getMaxRevenue()).toBe(500000);

    component.monthlyTrends = [{ monthName: 'A', revenue: 600000, profit: 0 }];
    expect(component.getMaxRevenue()).toBe(600000);
  });

  it('should compute bar height percentage clamped between 10 and 100', () => {
    fixture.detectChanges();
    component.monthlyTrends = [{ monthName: 'A', revenue: 500000, profit: 0 }];

    expect(component.getBarHeightPct(0)).toBe(10);
    expect(component.getBarHeightPct(500000)).toBe(100);
  });
});
