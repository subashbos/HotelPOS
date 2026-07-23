import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { LedgerComponent } from './ledger.component';
import { ReportService } from '../../../services/report.service';

describe('LedgerComponent', () => {
  let component: LedgerComponent;
  let fixture: ComponentFixture<LedgerComponent>;
  let reportServiceSpy: jasmine.SpyObj<ReportService>;

  beforeEach(async () => {
    reportServiceSpy = jasmine.createSpyObj('ReportService', ['getGstReport']);
    reportServiceSpy.getGstReport.and.returnValue(of([
      { sNo: 1, date: '2026-01-01', orderCount: 10, grossRevenue: 10000, gstAmount: 1800, netIncome: 8200 }
    ]));

    await TestBed.configureTestingModule({
      declarations: [LedgerComponent],
      imports: [FormsModule],
      providers: [
        { provide: ReportService, useValue: reportServiceSpy }
      ]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(LedgerComponent);
    component = fixture.componentInstance;
  });

  it('should create component and calculate totals', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
    expect(reportServiceSpy.getGstReport).toHaveBeenCalled();
    expect(component.rows.length).toBe(1);
    expect(component.totals).toEqual({ gross: 10000, gst: 1800, net: 8200 });
  });

  it('should handle load error', () => {
    spyOn(console, 'error');
    reportServiceSpy.getGstReport.and.returnValue(throwError(() => new Error('Error')));
    fixture.detectChanges();
    expect(component.isLoading).toBeFalse();
    expect(component.loadError).toBe('Failed to load the ledger. Please check the server connection.');
  });
});
