import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { PurchaseReportComponent } from './purchase-report.component';
import { ReportService } from '../../../services/report.service';
import { PagedPurchaseReport } from '../../../models/report.model';

describe('PurchaseReportComponent', () => {
  let component: PurchaseReportComponent;
  let fixture: ComponentFixture<PurchaseReportComponent>;
  let reportServiceSpy: jasmine.SpyObj<ReportService>;

  const mockReport: PagedPurchaseReport = {
    items: [],
    totalCount: 45,
    totalPurchases: 45,
    totalTax: 100,
    totalDiscount: 0,
    totalQty: 100
  };

  beforeEach(async () => {
    reportServiceSpy = jasmine.createSpyObj('ReportService', ['getPurchaseReport']);
    reportServiceSpy.getPurchaseReport.and.returnValue(of(mockReport));

    await TestBed.configureTestingModule({
      declarations: [PurchaseReportComponent],
      imports: [FormsModule],
      providers: [
        { provide: ReportService, useValue: reportServiceSpy }
      ]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(PurchaseReportComponent);
    component = fixture.componentInstance;
  });

  it('should create component and load purchase report', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
    expect(reportServiceSpy.getPurchaseReport).toHaveBeenCalled();
    expect(component.totalPages).toBe(3);
  });

  it('should handle pagination nextPage and prevPage', () => {
    fixture.detectChanges();
    component.nextPage();
    expect(component.page).toBe(2);

    component.prevPage();
    expect(component.page).toBe(1);
  });

  it('should handle load error', () => {
    spyOn(console, 'error');
    reportServiceSpy.getPurchaseReport.and.returnValue(throwError(() => new Error('Error')));
    fixture.detectChanges();
    expect(component.isLoading).toBeFalse();
    expect(component.loadError).toBe('Failed to load the purchase report. Please check the server connection.');
  });
});
