import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { ItemReportComponent } from './item-report.component';
import { ReportService } from '../../../services/report.service';

describe('ItemReportComponent', () => {
  let component: ItemReportComponent;
  let fixture: ComponentFixture<ItemReportComponent>;
  let reportServiceSpy: jasmine.SpyObj<ReportService>;

  beforeEach(async () => {
    reportServiceSpy = jasmine.createSpyObj('ReportService', ['getItemReport', 'exportItemReport']);
    reportServiceSpy.getItemReport.and.returnValue(of([
      { sNo: 1, itemId: 1, itemName: 'Burger', totalQtySold: 50, unitPrice: 150, totalRevenue: 7500 },
      { sNo: 2, itemId: 2, itemName: 'Pizza', totalQtySold: 30, unitPrice: 300, totalRevenue: 9000 }
    ]));

    await TestBed.configureTestingModule({
      declarations: [ItemReportComponent],
      imports: [FormsModule],
      providers: [
        { provide: ReportService, useValue: reportServiceSpy }
      ]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(ItemReportComponent);
    component = fixture.componentInstance;
  });

  it('should create component and calculate totalRevenue', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
    expect(reportServiceSpy.getItemReport).toHaveBeenCalled();
    expect(component.rows).toHaveSize(2);
    expect(component.totalRevenue).toBe(16500);
  });

  it('should handle load error', () => {
    spyOn(console, 'error');
    reportServiceSpy.getItemReport.and.returnValue(throwError(() => new Error('Error')));
    fixture.detectChanges();
    expect(component.isLoading).toBeFalse();
    expect(component.loadError).toBe('Failed to load the item report. Please check the server connection.');
  });

  it('should export the item report as a downloaded file', () => {
    spyOn(window.URL, 'createObjectURL').and.returnValue('blob:mock');
    spyOn(window.URL, 'revokeObjectURL');
    reportServiceSpy.exportItemReport.and.returnValue(of(new Blob(['data'])));
    fixture.detectChanges();

    component.export();

    expect(reportServiceSpy.exportItemReport).toHaveBeenCalledWith(component.fromDate, component.toDate);
    expect(component.isExporting).toBeFalse();
  });

  it('should handle export error', () => {
    spyOn(console, 'error');
    reportServiceSpy.exportItemReport.and.returnValue(throwError(() => new Error('Error')));
    fixture.detectChanges();

    component.export();

    expect(component.isExporting).toBeFalse();
  });
});
