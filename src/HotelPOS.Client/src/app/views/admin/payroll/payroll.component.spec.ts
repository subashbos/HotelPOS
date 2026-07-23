import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { PayrollComponent } from './payroll.component';
import { PayrollService } from '../../../services/payroll.service';
import { EmployeeService } from '../../../services/employee.service';
import { PayrollRun, SalaryStructure } from '../../../models/payroll.model';

describe('PayrollComponent', () => {
  let component: PayrollComponent;
  let fixture: ComponentFixture<PayrollComponent>;
  let payrollServiceSpy: jasmine.SpyObj<PayrollService>;
  let employeeServiceSpy: jasmine.SpyObj<EmployeeService>;

  const mockRun: PayrollRun = {
    id: 1,
    month: 1,
    year: 2026,
    status: 'Draft',
    processedOn: '2026-01-31',
    payslips: []
  };

  const mockStructure: SalaryStructure = {
    id: 10,
    employeeId: 1,
    effectiveFrom: '2026-01-01',
    basic: 20000,
    hra: 8000,
    da: 2000,
    conveyanceAllowance: 1600,
    medicalAllowance: 1250,
    specialAllowance: 2150,
    grossMonthly: 35000,
    pfApplicable: true,
    esiApplicable: false,
    professionalTaxApplicable: true
  };

  beforeEach(async () => {
    payrollServiceSpy = jasmine.createSpyObj('PayrollService', [
      'getRuns',
      'getRun',
      'getSalaryStructures',
      'saveSalaryStructure',
      'runPayroll',
      'markRunAsPaid'
    ]);
    employeeServiceSpy = jasmine.createSpyObj('EmployeeService', ['getEmployees']);

    payrollServiceSpy.getRuns.and.returnValue(of([mockRun]));
    payrollServiceSpy.getRun.and.returnValue(of(mockRun));
    payrollServiceSpy.getSalaryStructures.and.returnValue(of([mockStructure]));
    employeeServiceSpy.getEmployees.and.returnValue(of([{ id: 1, firstName: 'John', lastName: 'Doe', employeeCode: 'EMP001' } as any]));

    await TestBed.configureTestingModule({
      declarations: [PayrollComponent],
      imports: [FormsModule],
      providers: [
        { provide: PayrollService, useValue: payrollServiceSpy },
        { provide: EmployeeService, useValue: employeeServiceSpy }
      ]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(PayrollComponent);
    component = fixture.componentInstance;
  });

  it('should create component and load employees and runs', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
    expect(employeeServiceSpy.getEmployees).toHaveBeenCalled();
    expect(payrollServiceSpy.getSalaryStructures).toHaveBeenCalledWith(1);
    expect().toHaveSize();
    expect().toHaveSize();
  });

  it('should open and close salary form', () => {
    component.openSalaryForm();
    expect(component.showSalaryForm).toBeTrue();

    component.closeSalaryForm();
    expect(component.showSalaryForm).toBeFalse();
  });

  it('should calculate grossMonthly', () => {
    component.salaryForm.basic = 10000;
    component.salaryForm.hra = 4000;
    component.salaryForm.da = 1000;
    expect(component.grossMonthly).toBe(15000);
  });

  it('should save salary structure', () => {
    payrollServiceSpy.saveSalaryStructure.and.returnValue(of(mockStructure));
    fixture.detectChanges();
    component.openSalaryForm();
    component.salaryForm.basic = 25000;

    component.saveSalaryStructure();

    expect(payrollServiceSpy.saveSalaryStructure).toHaveBeenCalled();
    expect(component.showSalaryForm).toBeFalse();
  });

  it('should run payroll', () => {
    payrollServiceSpy.runPayroll.and.returnValue(of(mockRun));
    component.runMonth = 1;
    component.runYear = 2026;

    component.runPayroll();

    expect(payrollServiceSpy.runPayroll).toHaveBeenCalledWith(1, 2026);
    expect(component.selectedRun).toEqual(mockRun);
  });

  it('should view run and mark as paid', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    payrollServiceSpy.markRunAsPaid.and.returnValue(of(void 0));

    component.viewRun(mockRun);
    expect(payrollServiceSpy.getRun).toHaveBeenCalledWith(1);

    component.markPaid(mockRun);
    expect(payrollServiceSpy.markRunAsPaid).toHaveBeenCalledWith(1);
  });

  it('should track structure and run by id', () => {
    expect(component.trackByStructureId(0, mockStructure)).toBe(10);
    expect(component.trackByRunId(0, mockRun)).toBe(1);
  });
});
