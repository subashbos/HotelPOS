import { TestBed, ComponentFixture } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { EssPayslipsComponent } from './ess-payslips.component';
import { EssService } from '../../../services/ess.service';
import { Payslip } from '../../../models/payroll.model';

describe('EssPayslipsComponent', () => {
  let component: EssPayslipsComponent;
  let fixture: ComponentFixture<EssPayslipsComponent>;
  let essServiceSpy: jasmine.SpyObj<EssService>;

  const mockPayslip: Payslip = {
    id: 1, payrollRunId: 1, employeeId: 1, grossEarnings: 50000, workingDays: 30, paidDays: 30, lopDays: 0
  } as Payslip;

  beforeEach(async () => {
    essServiceSpy = jasmine.createSpyObj('EssService', ['getPayslips']);
    essServiceSpy.getPayslips.and.returnValue(of([mockPayslip]));

    await TestBed.configureTestingModule({
      declarations: [EssPayslipsComponent],
      providers: [{ provide: EssService, useValue: essServiceSpy }]
    }).compileComponents();

    fixture = TestBed.createComponent(EssPayslipsComponent);
    component = fixture.componentInstance;
  });

  it('should create and load payslips', () => {
    fixture.detectChanges();

    expect(component).toBeTruthy();
    expect(component.payslips).toEqual([mockPayslip]);
    expect(component.isLoading).toBeFalse();
  });

  it('should handle load error', () => {
    spyOn(console, 'error');
    essServiceSpy.getPayslips.and.returnValue(throwError(() => new Error('boom')));

    fixture.detectChanges();

    expect(component.isLoading).toBeFalse();
    expect(component.loadError).toContain('Failed to load your payslips');
  });

  it('should track payslips by id', () => {
    expect(component.trackByPayslipId(0, mockPayslip)).toBe(1);
  });
});
