import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { SalesReportComponent } from './sales-report.component';
import { ReportService } from '../../../services/report.service';

describe('SalesReportComponent', () => {
  let component: SalesReportComponent;
  let fixture: ComponentFixture<SalesReportComponent>;
  let reportServiceSpy: jasmine.SpyObj<ReportService>;

  beforeEach(async () => {
    reportServiceSpy = jasmine.createSpyObj('ReportService', ['getSalesReport', 'exportSalesReport']);
    reportServiceSpy.getSalesReport.and.returnValue(of({
      totalRevenue: 0,
      totalOrders: 0,
      averageOrderValue: 0,
      mostPopularItem: 'N/A',
      salesByTable: [],
      recentOrders: [],
      salesByCategory: [],
      salesByPaymentMode: []
    }));

    await TestBed.configureTestingModule({
      declarations: [SalesReportComponent],
      imports: [FormsModule],
      providers: [
        { provide: ReportService, useValue: reportServiceSpy }
      ]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(SalesReportComponent);
    component = fixture.componentInstance;
  });

  it('should create component and load sales report', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
    expect(reportServiceSpy.getSalesReport).toHaveBeenCalled();
  });

  it('should export the sales report as a downloaded file', () => {
    spyOn(window.URL, 'createObjectURL').and.returnValue('blob:mock');
    spyOn(window.URL, 'revokeObjectURL');
    const blob = new Blob(['data']);
    reportServiceSpy.exportSalesReport.and.returnValue(of(blob));
    fixture.detectChanges();

    component.export();

    expect(reportServiceSpy.exportSalesReport).toHaveBeenCalledWith(component.fromDate, component.toDate);
    expect(component.isExporting).toBeFalse();
  });

  it('should handle export error', () => {
    spyOn(console, 'error');
    reportServiceSpy.exportSalesReport.and.returnValue(throwError(() => new Error('Error')));
    fixture.detectChanges();

    component.export();

    expect(component.isExporting).toBeFalse();
  });
});
