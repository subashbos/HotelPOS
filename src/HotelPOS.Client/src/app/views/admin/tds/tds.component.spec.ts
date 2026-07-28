import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { TdsComponent } from './tds.component';
import { TdsService } from '../../../services/tds.service';
import { TdsRuleSet } from '../../../models/tds.model';

describe('TdsComponent', () => {
  let component: TdsComponent;
  let fixture: ComponentFixture<TdsComponent>;
  let tdsServiceSpy: jasmine.SpyObj<TdsService>;

  const mockRuleSet: TdsRuleSet = {
    financialYearStart: 2025,
    standardDeduction: 75000,
    rebateIncomeLimit: 700000,
    cessRatePercent: 4,
    slabs: [{ incomeFrom: 0, incomeTo: 300000, ratePercent: 0, displayOrder: 1 }]
  };

  beforeEach(async () => {
    tdsServiceSpy = jasmine.createSpyObj('TdsService', [
      'getFinancialYears', 'getRuleSet', 'saveRuleSet', 'deleteRuleSet'
    ]);
    tdsServiceSpy.getFinancialYears.and.returnValue(of([2025]));
    tdsServiceSpy.getRuleSet.and.returnValue(of(mockRuleSet));

    await TestBed.configureTestingModule({
      declarations: [TdsComponent],
      imports: [FormsModule],
      providers: [{ provide: TdsService, useValue: tdsServiceSpy }]
    }).compileComponents();

    fixture = TestBed.createComponent(TdsComponent);
    component = fixture.componentInstance;
  });

  it('should create and load the first financial year on init', () => {
    fixture.detectChanges();

    expect(component).toBeTruthy();
    expect(tdsServiceSpy.getFinancialYears).toHaveBeenCalled();
    expect(component.selectedYear).toBe(2025);
    expect(component.standardDeduction).toBe(75000);
    expect(component.slabs.length).toBe(1);
  });

  it('should handle load error for financial years', () => {
    spyOn(console, 'error');
    tdsServiceSpy.getFinancialYears.and.returnValue(throwError(() => new Error('boom')));

    fixture.detectChanges();

    expect(component.isLoading).toBeFalse();
    expect(component.loadError).toContain('Failed to load TDS financial years');
  });

  it('should leave selectedYear null when there are no financial years', () => {
    tdsServiceSpy.getFinancialYears.and.returnValue(of([]));

    fixture.detectChanges();

    expect(component.selectedYear).toBeNull();
  });

  it('should handle rule set load error', () => {
    spyOn(console, 'error');
    tdsServiceSpy.getRuleSet.and.returnValue(throwError(() => new Error('boom')));

    fixture.detectChanges();

    expect(component.actionError).toContain('Failed to load the selected financial year');
  });

  it('should parse year from onYearChange and select it', () => {
    fixture.detectChanges();
    tdsServiceSpy.getRuleSet.calls.reset();

    component.onYearChange('2025');

    expect(tdsServiceSpy.getRuleSet).toHaveBeenCalledWith(2025);
  });

  it('should ignore invalid onYearChange input', () => {
    fixture.detectChanges();
    tdsServiceSpy.getRuleSet.calls.reset();

    component.onYearChange('not-a-number');

    expect(tdsServiceSpy.getRuleSet).not.toHaveBeenCalled();
  });

  it('should open, close, and create a new year form', () => {
    fixture.detectChanges();

    component.openNewYearForm();
    expect(component.showNewYearForm).toBeTrue();
    expect(component.newYearValue).not.toBeNull();

    component.createNewYear();
    expect(component.showNewYearForm).toBeFalse();
    expect(component.selectedYear).toBe(component.newYearValue ?? component.selectedYear);
    expect(component.slabs.length).toBe(1);
    expect(component.slabs[0].incomeTo).toBeNull();

    component.closeNewYearForm();
    expect(component.showNewYearForm).toBeFalse();
  });

  it('should not create a new year when newYearValue is unset', () => {
    fixture.detectChanges();
    component.newYearValue = null;
    const slabsBefore = component.slabs;

    component.createNewYear();

    expect(component.slabs).toBe(slabsBefore);
  });

  it('should add and remove slab rows, keeping displayOrder sequential', () => {
    fixture.detectChanges();
    component.slabs = [{ incomeFrom: 0, incomeTo: 100, ratePercent: 5, displayOrder: 1 }];

    component.addSlabRow();
    expect(component.slabs.length).toBe(2);
    expect(component.slabs[1].incomeFrom).toBe(100);

    component.removeSlabRow(0);
    expect(component.slabs.length).toBe(1);
    expect(component.slabs[0].displayOrder).toBe(1);
  });

  it('should save the rule set and reload years', () => {
    fixture.detectChanges();
    tdsServiceSpy.saveRuleSet.and.returnValue(of(void 0));

    component.save();

    expect(tdsServiceSpy.saveRuleSet).toHaveBeenCalledWith(2025, {
      standardDeduction: component.standardDeduction,
      rebateIncomeLimit: component.rebateIncomeLimit,
      cessRatePercent: component.cessRatePercent,
      slabs: component.slabs
    });
    expect(component.isSaving).toBeFalse();
  });

  it('should not save when no year is selected or already saving', () => {
    fixture.detectChanges();
    component.selectedYear = null;

    component.save();

    expect(tdsServiceSpy.saveRuleSet).not.toHaveBeenCalled();
  });

  it('should surface save errors', () => {
    spyOn(console, 'error');
    fixture.detectChanges();
    tdsServiceSpy.saveRuleSet.and.returnValue(throwError(() => ({ error: { message: 'Save failed' } })));

    component.save();

    expect(component.isSaving).toBeFalse();
    expect(component.actionError).toBe('Save failed');
  });

  it('should delete the year when confirmed', () => {
    fixture.detectChanges();
    spyOn(window, 'confirm').and.returnValue(true);
    tdsServiceSpy.deleteRuleSet.and.returnValue(of(void 0));

    component.deleteYear();

    expect(tdsServiceSpy.deleteRuleSet).toHaveBeenCalledWith(2025);
  });

  it('should not delete the year when not confirmed', () => {
    fixture.detectChanges();
    spyOn(window, 'confirm').and.returnValue(false);

    component.deleteYear();

    expect(tdsServiceSpy.deleteRuleSet).not.toHaveBeenCalled();
  });

  it('should surface delete errors', () => {
    spyOn(console, 'error');
    fixture.detectChanges();
    spyOn(window, 'confirm').and.returnValue(true);
    tdsServiceSpy.deleteRuleSet.and.returnValue(throwError(() => ({ error: { message: 'Delete failed' } })));

    component.deleteYear();

    expect(component.actionError).toBe('Delete failed');
  });

  it('should track slabs by index', () => {
    expect(component.trackBySlabIndex(2)).toBe(2);
  });
});
