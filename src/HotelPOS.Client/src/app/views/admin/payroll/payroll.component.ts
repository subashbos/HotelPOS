import { Component, OnInit } from '@angular/core';
import { PayrollService } from '../../../services/payroll.service';
import { EmployeeService } from '../../../services/employee.service';
import { PayrollRun, SalaryStructure } from '../../../models/payroll.model';
import { Employee } from '../../../models/employee.model';

interface SalaryFormModel {
  effectiveFrom: string;
  basic: number;
  hra: number;
  da: number;
  conveyanceAllowance: number;
  medicalAllowance: number;
  specialAllowance: number;
  pfApplicable: boolean;
  esiApplicable: boolean;
  professionalTaxApplicable: boolean;
}

function emptySalaryForm(): SalaryFormModel {
  return {
    effectiveFrom: new Date().toISOString().substring(0, 10),
    basic: 0,
    hra: 0,
    da: 0,
    conveyanceAllowance: 0,
    medicalAllowance: 0,
    specialAllowance: 0,
    pfApplicable: true,
    esiApplicable: false,
    professionalTaxApplicable: true
  };
}

@Component({
  standalone: false,
  selector: 'app-payroll',
  templateUrl: './payroll.component.html',
})
export class PayrollComponent implements OnInit {
  employees: Employee[] = [];
  salaryStructures: SalaryStructure[] = [];
  runs: PayrollRun[] = [];
  selectedRun: PayrollRun | null = null;

  isLoading = false;
  loadError = '';
  actionError = '';
  isSaving = false;

  selectedEmployeeId: number | null = null;

  showSalaryForm = false;
  salaryForm: SalaryFormModel = emptySalaryForm();

  runMonth = new Date().getMonth() + 1;
  runYear = new Date().getFullYear();
  isRunningPayroll = false;

  constructor(
    private readonly payrollService: PayrollService,
    private readonly employeeService: EmployeeService
  ) {}

  ngOnInit(): void {
    this.employeeService.getEmployees().subscribe({
      next: (employees) => {
        this.employees = employees;
        if (employees.length > 0) {
          this.selectedEmployeeId = employees[0].id;
          this.loadSalaryStructures();
        }
      },
      error: (err) => console.error('Employees load error:', err)
    });
    this.loadRuns();
  }

  loadSalaryStructures(): void {
    if (!this.selectedEmployeeId) return;
    this.isLoading = true;
    this.loadError = '';
    this.payrollService.getSalaryStructures(this.selectedEmployeeId).subscribe({
      next: (structures) => {
        this.salaryStructures = structures.sort((a, b) => (a.effectiveFrom < b.effectiveFrom ? 1 : -1));
        this.isLoading = false;
      },
      error: (err) => {
        this.loadError = 'Failed to load salary structures. Please check the server connection.';
        this.isLoading = false;
        console.error('Salary structures load error:', err);
      }
    });
  }

  loadRuns(): void {
    this.payrollService.getRuns().subscribe({
      next: (runs) => (this.runs = runs.sort((a, b) => (a.year !== b.year ? b.year - a.year : b.month - a.month))),
      error: (err) => console.error('Payroll runs load error:', err)
    });
  }

  openSalaryForm(): void {
    this.salaryForm = emptySalaryForm();
    this.actionError = '';
    this.showSalaryForm = true;
  }

  closeSalaryForm(): void {
    this.showSalaryForm = false;
  }

  get grossMonthly(): number {
    const f = this.salaryForm;
    return f.basic + f.hra + f.da + f.conveyanceAllowance + f.medicalAllowance + f.specialAllowance;
  }

  saveSalaryStructure(): void {
    if (!this.selectedEmployeeId || this.salaryForm.basic <= 0 || this.isSaving) return;
    this.isSaving = true;
    this.actionError = '';

    this.payrollService.saveSalaryStructure({
      id: 0,
      employeeId: this.selectedEmployeeId,
      ...this.salaryForm
    }).subscribe({
      next: () => {
        this.isSaving = false;
        this.closeSalaryForm();
        this.loadSalaryStructures();
      },
      error: (err) => {
        this.isSaving = false;
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to save salary structure.';
        console.error('Salary structure save error:', err);
      }
    });
  }

  runPayroll(): void {
    if (this.isRunningPayroll) return;
    this.isRunningPayroll = true;
    this.actionError = '';
    this.payrollService.runPayroll(this.runMonth, this.runYear).subscribe({
      next: (run) => {
        this.isRunningPayroll = false;
        this.selectedRun = run;
        this.loadRuns();
      },
      error: (err) => {
        this.isRunningPayroll = false;
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to run payroll.';
        console.error('Run payroll error:', err);
      }
    });
  }

  viewRun(run: PayrollRun): void {
    this.payrollService.getRun(run.id).subscribe({
      next: (fullRun) => (this.selectedRun = fullRun),
      error: (err) => console.error('Payroll run load error:', err)
    });
  }

  markPaid(run: PayrollRun): void {
    if (!confirm(`Mark payroll run ${run.month}/${run.year} as paid?`)) return;
    this.payrollService.markRunAsPaid(run.id).subscribe({
      next: () => {
        this.loadRuns();
        if (this.selectedRun?.id === run.id) this.viewRun(run);
      },
      error: (err) => {
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to mark run as paid.';
        console.error('Mark run paid error:', err);
      }
    });
  }

  trackByStructureId(_index: number, s: SalaryStructure): number {
    return s.id;
  }

  trackByRunId(_index: number, run: PayrollRun): number {
    return run.id;
  }
}
