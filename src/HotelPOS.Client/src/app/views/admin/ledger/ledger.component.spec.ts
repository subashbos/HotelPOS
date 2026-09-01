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
    reportServiceSpy = jasmine.createSpyObj('ReportService', ['getLedgerReport', 'exportLedgerReport']);
    reportServiceSpy.getLedgerReport.and.returnValue(of([
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
    expect(reportServiceSpy.getLedgerReport).toHaveBeenCalled();
    expect(component.rows).toHaveSize(1);
    expect(component.totals).toEqual({ gross: 10000, gst: 1800, net: 8200 });
  });

  it('should handle load error', () => {
    spyOn(console, 'error');
    reportServiceSpy.getLedgerReport.and.returnValue(throwError(() => new Error('Error')));
    fixture.detectChanges();
    expect(component.isLoading).toBeFalse();
    expect(component.loadError).toBe('Failed to load the ledger. Please check the server connection.');
  });

  it('should export the ledger as a downloaded file', () => {
    spyOn(window.URL, 'createObjectURL').and.returnValue('blob:mock');
    spyOn(window.URL, 'revokeObjectURL');
    reportServiceSpy.exportLedgerReport.and.returnValue(of(new Blob(['data'])));
    fixture.detectChanges();

    component.export();

    expect(reportServiceSpy.exportLedgerReport).toHaveBeenCalledWith(component.fromDate, component.toDate);
    expect(component.isExporting).toBeFalse();
  });

  it('should handle export error', () => {
    spyOn(console, 'error');
    reportServiceSpy.exportLedgerReport.and.returnValue(throwError(() => new Error('Error')));
    fixture.detectChanges();

    component.export();

    expect(component.isExporting).toBeFalse();
  });
});
