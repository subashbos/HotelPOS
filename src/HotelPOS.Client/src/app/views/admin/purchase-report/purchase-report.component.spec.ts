import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { PurchaseReportComponent } from './purchase-report.component';
import { ReportService } from '../../../services/report.service';
import { SupplierService } from '../../../services/supplier.service';
import { PagedPurchaseReport } from '../../../models/report.model';
import { Supplier } from '../../../models/supplier.model';

describe('PurchaseReportComponent', () => {
  let component: PurchaseReportComponent;
  let fixture: ComponentFixture<PurchaseReportComponent>;
  let reportServiceSpy: jasmine.SpyObj<ReportService>;
  let supplierServiceSpy: jasmine.SpyObj<SupplierService>;

  const mockReport: PagedPurchaseReport = {
    items: [],
    totalCount: 45,
    totalPurchases: 45,
    totalTax: 100,
    totalDiscount: 0,
    totalQty: 100
  };

  beforeEach(async () => {
    reportServiceSpy = jasmine.createSpyObj('ReportService', ['getPurchaseReport', 'exportPurchaseReport']);
    reportServiceSpy.getPurchaseReport.and.returnValue(of(mockReport));
    supplierServiceSpy = jasmine.createSpyObj('SupplierService', ['getSuppliers']);
    supplierServiceSpy.getSuppliers.and.returnValue(of([] as Supplier[]));

    await TestBed.configureTestingModule({
      declarations: [PurchaseReportComponent],
      imports: [FormsModule],
      providers: [
        { provide: ReportService, useValue: reportServiceSpy },
        { provide: SupplierService, useValue: supplierServiceSpy }
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

  it('should export the purchase report as a downloaded file', () => {
    spyOn(window.URL, 'createObjectURL').and.returnValue('blob:mock');
    spyOn(window.URL, 'revokeObjectURL');
    reportServiceSpy.exportPurchaseReport.and.returnValue(of(new Blob(['data'])));
    fixture.detectChanges();

    component.export();

    expect(reportServiceSpy.exportPurchaseReport).toHaveBeenCalled();
    expect(component.isExporting).toBeFalse();
  });

  it('should handle export error', () => {
    spyOn(console, 'error');
    reportServiceSpy.exportPurchaseReport.and.returnValue(throwError(() => new Error('Error')));
    fixture.detectChanges();

    component.export();

    expect(component.isExporting).toBeFalse();
  });

  it('should load suppliers for the filter dropdown on init', () => {
    const suppliers: Supplier[] = [{ id: 1, name: 'Acme Foods' } as Supplier];
    supplierServiceSpy.getSuppliers.and.returnValue(of(suppliers));
    fixture.detectChanges();

    expect(component.suppliers).toEqual(suppliers);
  });

  it('should reset to page 1 and reload when filters are applied', () => {
    fixture.detectChanges();
    component.page = 3;
    component.supplierId = 5;
    component.itemName = 'Rice';
    component.paymentType = 'Credit';
    component.invoiceNo = 'INV-1';

    component.applyFilters();

    expect(component.page).toBe(1);
    expect(reportServiceSpy.getPurchaseReport).toHaveBeenCalledWith(
      1, 20, undefined, undefined,
      { supplierId: 5, itemName: 'Rice', paymentType: 'Credit', invoiceNo: 'INV-1' }
    );
  });

  it('should clear filters and reload', () => {
    fixture.detectChanges();
    component.supplierId = 5;
    component.itemName = 'Rice';
    component.paymentType = 'Credit';
    component.invoiceNo = 'INV-1';

    component.clearFilters();

    expect(component.supplierId).toBeNull();
    expect(component.itemName).toBe('');
    expect(component.paymentType).toBe('');
    expect(component.invoiceNo).toBe('');
    expect(reportServiceSpy.getPurchaseReport).toHaveBeenCalledWith(
      1, 20, undefined, undefined,
      { supplierId: undefined, itemName: undefined, paymentType: undefined, invoiceNo: undefined }
    );
  });
});
