import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of } from 'rxjs';
import { BiAnalyticsComponent } from './bi-analytics.component';
import { BiAnalyticsService } from '../../../services/bi-analytics.service';
import { ReportService } from '../../../services/report.service';

describe('BiAnalyticsComponent', () => {
  let component: BiAnalyticsComponent;
  let fixture: ComponentFixture<BiAnalyticsComponent>;
  let biServiceSpy: jasmine.SpyObj<BiAnalyticsService>;
  let reportServiceSpy: jasmine.SpyObj<ReportService>;

  beforeEach(async () => {
    biServiceSpy = jasmine.createSpyObj('BiAnalyticsService', ['getBiAnalytics']);
    reportServiceSpy = jasmine.createSpyObj('ReportService', [
      'getShiftClosureReport', 'getVoidDiscountAudit', 'getStaffPerformanceReport',
      'getStockValuationReport', 'getProfitAndLossReport'
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
