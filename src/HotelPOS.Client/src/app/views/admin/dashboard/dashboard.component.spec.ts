import { TestBed, ComponentFixture } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { DashboardComponent } from './dashboard.component';
import { ReportService } from '../../../services/report.service';

describe('DashboardComponent', () => {
  let component: DashboardComponent;
  let fixture: ComponentFixture<DashboardComponent>;
  let reportServiceSpy: jasmine.SpyObj<ReportService>;

  beforeEach(async () => {
    reportServiceSpy = jasmine.createSpyObj('ReportService', [
      'getSalesReport',
      'getMonthlyChart',
      'getLowStockAlerts'
    ]);

    await TestBed.configureTestingModule({
      declarations: [DashboardComponent],
      providers: [
        { provide: ReportService, useValue: reportServiceSpy }
      ]
    }).compileComponents();
  });

  beforeEach(() => {
    reportServiceSpy.getSalesReport.and.returnValue(of({
      totalRevenue: 15000,
      totalOrders: 120,
      averageOrderValue: 125,
      mostPopularItem: 'Burger',
      salesByTable: [],
      recentOrders: [],
      salesByCategory: [],
      salesByPaymentMode: []
    }));
    reportServiceSpy.getMonthlyChart.and.returnValue(of([
      { monthName: 'Jan 2026', revenue: 50000 },
      { monthName: 'Feb 2026', revenue: 65000 }
    ]));
    reportServiceSpy.getLowStockAlerts.and.returnValue(of([
      { sNo: 1, itemId: 1, itemName: 'Milk', currentStock: 2, minThreshold: 5, dailyConsumptionRate: 1, daysRemaining: 2, alertLevel: 'Low' },
      { sNo: 2, itemId: 2, itemName: 'Sugar', currentStock: 50, minThreshold: 5, dailyConsumptionRate: 1, daysRemaining: 50, alertLevel: 'Normal' }
    ]));

    fixture = TestBed.createComponent(DashboardComponent);
    component = fixture.componentInstance;
  });

  it('should create component', () => {
    expect(component).toBeTruthy();
  });

  it('should load report, monthly chart, and low stock alerts on init', () => {
    fixture.detectChanges();

    expect(reportServiceSpy.getSalesReport).toHaveBeenCalled();
    expect(reportServiceSpy.getMonthlyChart).toHaveBeenCalled();
    expect(reportServiceSpy.getLowStockAlerts).toHaveBeenCalled();
    expect(component.report?.totalRevenue).toBe(15000);
    expect(component.monthlyChart.length).toBe(2);
    expect(component.lowStock.length).toBe(1); // filtered out 'Normal'
    expect(component.isLoading).toBeFalse();
  });

  it('should handle sales report load error', () => {
    reportServiceSpy.getSalesReport.and.returnValue(throwError(() => new Error('Server error')));

    fixture.detectChanges();

    expect(component.isLoading).toBeFalse();
    expect(component.loadError).toBe('Failed to load dashboard data. Please check the server connection.');
  });

  it('should calculate maxMonthlyRevenue accurately', () => {
    component.monthlyChart = [
      { monthName: 'Jan', revenue: 100 },
      { monthName: 'Feb', revenue: 350 }
    ];
    expect(component.maxMonthlyRevenue).toBe(350);
  });
});
