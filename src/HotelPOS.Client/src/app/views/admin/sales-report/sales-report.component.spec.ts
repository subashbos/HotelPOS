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
    reportServiceSpy = jasmine.createSpyObj('ReportService', ['getSalesReport']);
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
});
