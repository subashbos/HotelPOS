import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { JournalComponent } from './journal.component';
import { ReportService } from '../../../services/report.service';

describe('JournalComponent', () => {
  let component: JournalComponent;
  let fixture: ComponentFixture<JournalComponent>;
  let reportServiceSpy: jasmine.SpyObj<ReportService>;

  beforeEach(async () => {
    reportServiceSpy = jasmine.createSpyObj('ReportService', [
      'getMarginSummary',
      'getItemMargins',
      'getWastageSummary',
      'getLowStockAlerts',
      'getMonthlyTrend'
    ]);

    reportServiceSpy.getMarginSummary.and.returnValue(of({ totalRevenue: 0, totalCogs: 0, grossProfit: 0, totalExpenses: 0, netProfit: 0, marginPercentage: 0, foodCostPercentage: 0 }));
    reportServiceSpy.getItemMargins.and.returnValue(of([]));
    reportServiceSpy.getWastageSummary.and.returnValue(of({ totalWastageCost: 0, totalWastageQty: 0, reasonsBreakdown: [], recentWastage: [] }));
    reportServiceSpy.getLowStockAlerts.and.returnValue(of([]));
    reportServiceSpy.getMonthlyTrend.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      declarations: [JournalComponent],
      imports: [FormsModule],
      providers: [
        { provide: ReportService, useValue: reportServiceSpy }
      ]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(JournalComponent);
    component = fixture.componentInstance;
  });

  it('should create component and load journal data', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
    expect(reportServiceSpy.getMarginSummary).toHaveBeenCalled();
  });
});
