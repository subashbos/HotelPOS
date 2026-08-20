import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { Gstr1ReportComponent } from './gstr1-report.component';
import { ReportService } from '../../../services/report.service';
import { GstR1Report } from '../../../models/report.model';

describe('Gstr1ReportComponent', () => {
  let component: Gstr1ReportComponent;
  let fixture: ComponentFixture<Gstr1ReportComponent>;
  let reportServiceSpy: jasmine.SpyObj<ReportService>;

  const dummyReport: GstR1Report = {
    b2BRows: [
      { sNo: 1, gstin: '33AQZPS2365E1ZE', invoiceNumber: 'INV1', date: '2026-07-01', invoiceValue: 6615,
        pos: '33', reverseCharge: 'N', invoiceType: 'R', customerName: 'Anu Labs', taxableValue: 6300,
        itemTotal: 6615, rate: 5, cgst: 157.5, sgst: 157.5, igst: 0 },
      { sNo: 2, gstin: '29APWAS2365E1ZE', invoiceNumber: 'INV2', date: '2026-07-02', invoiceValue: 2000,
        pos: '29', reverseCharge: 'N', invoiceType: 'R', customerName: 'Other Corp', taxableValue: 1800,
        itemTotal: 2000, rate: 18, cgst: 100, sgst: 100, igst: 0 }
    ],
    b2cSummary: [
      { rate: 5, invoiceCount: 2, taxableValue: 7300, cgst: 182.5, sgst: 182.5, igst: 0, totalTax: 365, totalValue: 7665 }
    ],
    hsnSummary: [
      { hsnCode: '2106', description: 'Chicken Biriyani', uqc: 'Plate', totalQuantity: 3, taxableValue: 300,
        rate: 5, cgst: 7.5, sgst: 7.5, igst: 0, totalTax: 15, totalValue: 315 }
    ]
  };

  beforeEach(async () => {
    reportServiceSpy = jasmine.createSpyObj('ReportService', ['getGstR1Report']);
    reportServiceSpy.getGstR1Report.and.returnValue(of(dummyReport));

    await TestBed.configureTestingModule({
      declarations: [Gstr1ReportComponent],
      imports: [FormsModule],
      providers: [
        { provide: ReportService, useValue: reportServiceSpy }
      ]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(Gstr1ReportComponent);
    component = fixture.componentInstance;
  });

  it('should create component and load all three tables', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
    expect(reportServiceSpy.getGstR1Report).toHaveBeenCalled();
    expect(component.b2bRows).toHaveSize(2);
    expect(component.b2cSummary).toHaveSize(1);
    expect(component.hsnSummary).toHaveSize(1);
  });

  it('should compute B2B summary badges from distinct GSTINs and invoices', () => {
    fixture.detectChanges();
    expect(component.distinctRecipients).toBe(2);
    expect(component.distinctInvoices).toBe(2);
    expect(component.b2bTaxableValue).toBe(8100);
    expect(component.b2bTaxAmount).toBe(515);
  });

  it('should compute B2C summary badges', () => {
    fixture.detectChanges();
    expect(component.b2cInvoiceCount).toBe(2);
    expect(component.b2cTaxableValue).toBe(7300);
    expect(component.b2cTaxAmount).toBe(365);
  });

  it('should compute HSN summary badges', () => {
    fixture.detectChanges();
    expect(component.hsnCodeCount).toBe(1);
    expect(component.hsnTotalQuantity).toBe(3);
    expect(component.hsnTaxableValue).toBe(300);
  });

  it('should switch tabs', () => {
    fixture.detectChanges();
    expect(component.activeTab).toBe('b2b');
    component.setTab('hsn');
    expect(component.activeTab).toBe('hsn');
  });

  it('should handle load error', () => {
    spyOn(console, 'error');
    reportServiceSpy.getGstR1Report.and.returnValue(throwError(() => new Error('Error')));
    fixture.detectChanges();
    expect(component.isLoading).toBeFalse();
    expect(component.loadError).toBe('Failed to load the GSTR-1 report. Please check the server connection.');
  });
});
