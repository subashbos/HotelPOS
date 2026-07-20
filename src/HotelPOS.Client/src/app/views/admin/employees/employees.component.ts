import { Component, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { EmployeeService } from '../../../services/employee.service';
import {
  Department, Designation, Employee, EMPLOYEE_STATUSES, EMPLOYMENT_TYPES, SaveEmployeeRequest
} from '../../../models/employee.model';

type EmployeeFormModel = Omit<SaveEmployeeRequest, 'id'>;

function emptyForm(): EmployeeFormModel {
  return {
    employeeCode: '',
    firstName: '',
    lastName: '',
    gender: '',
    dateOfBirth: undefined,
    dateOfJoining: new Date().toISOString().substring(0, 10),
    dateOfExit: undefined,
    departmentId: undefined,
    designationId: undefined,
    employmentType: EMPLOYMENT_TYPES[0],
    status: EMPLOYEE_STATUSES[0],
    phone: '',
    email: '',
    address: '',
    pan: '',
    aadhaar: '',
    uan: '',
    esicNumber: '',
    bankName: '',
    bankAccountNumber: '',
    bankIfsc: '',
    emergencyContactName: '',
    emergencyContactPhone: '',
    reportingManagerId: undefined
  };
}

@Component({
  standalone: false,
  selector: 'app-employees',
  templateUrl: './employees.component.html',
})
export class EmployeesComponent implements OnInit {
  employees: Employee[] = [];
  departments: Department[] = [];
  designations: Designation[] = [];

  isLoading = false;
  loadError = '';
  actionError = '';
  isSaving = false;

  readonly employmentTypes = EMPLOYMENT_TYPES;
  readonly statuses = EMPLOYEE_STATUSES;

  showForm = false;
  editingId: number | null = null;
  form: EmployeeFormModel = emptyForm();

  constructor(private readonly employeeService: EmployeeService) {}

  ngOnInit(): void {
    this.loadEmployees();
    this.employeeService.getDepartments().subscribe({
      next: (departments) => (this.departments = departments),
      error: (err) => console.error('Departments load error:', err)
    });
    this.employeeService.getDesignations().subscribe({
      next: (designations) => (this.designations = designations),
      error: (err) => console.error('Designations load error:', err)
    });
  }

  loadEmployees(): void {
    this.isLoading = true;
    this.loadError = '';
    this.employeeService.getEmployees().subscribe({
      next: (employees) => {
        this.employees = employees;
        this.isLoading = false;
      },
      error: (err) => {
        this.loadError = 'Failed to load employees. Please check the server connection.';
        this.isLoading = false;
        console.error('Employees load error:', err);
      }
    });
  }

  designationsFor(departmentId?: number): Designation[] {
    if (!departmentId) return this.designations;
    return this.designations.filter((d) => d.departmentId === departmentId);
  }

  openAddForm(): void {
    this.editingId = null;
    this.form = emptyForm();
    this.actionError = '';
    this.showForm = true;
  }

  openEditForm(employee: Employee): void {
    this.editingId = employee.id;
    this.form = {
      employeeCode: employee.employeeCode,
      firstName: employee.firstName,
      lastName: employee.lastName ?? '',
      gender: employee.gender ?? '',
      dateOfBirth: employee.dateOfBirth?.substring(0, 10),
      dateOfJoining: employee.dateOfJoining.substring(0, 10),
      dateOfExit: employee.dateOfExit?.substring(0, 10),
      departmentId: employee.departmentId,
      designationId: employee.designationId,
      employmentType: employee.employmentType,
      status: employee.status,
      phone: employee.phone ?? '',
      email: employee.email ?? '',
      address: employee.address ?? '',
      pan: employee.pan ?? '',
      aadhaar: employee.aadhaar ?? '',
      uan: employee.uan ?? '',
      esicNumber: employee.esicNumber ?? '',
      bankName: employee.bankName ?? '',
      bankAccountNumber: employee.bankAccountNumber ?? '',
      bankIfsc: employee.bankIfsc ?? '',
      emergencyContactName: employee.emergencyContactName ?? '',
      emergencyContactPhone: employee.emergencyContactPhone ?? '',
      reportingManagerId: employee.reportingManagerId
    };
    this.actionError = '';
    this.showForm = true;
  }

  closeForm(): void {
    this.showForm = false;
    this.editingId = null;
  }

  save(): void {
    if (!this.form.firstName.trim() || !this.form.dateOfJoining || this.isSaving) return;
    this.isSaving = true;
    this.actionError = '';

    const request$: Observable<unknown> = this.editingId
      ? this.employeeService.updateEmployee(this.editingId, { id: this.editingId, ...this.form })
      : this.employeeService.createEmployee({ id: 0, ...this.form });

    request$.subscribe({
      next: () => {
        this.isSaving = false;
        this.closeForm();
        this.loadEmployees();
      },
      error: (err) => {
        this.isSaving = false;
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to save employee.';
        console.error('Employee save error:', err);
      }
    });
  }

  deleteEmployee(employee: Employee): void {
    if (!confirm(`Delete employee "${employee.firstName} ${employee.lastName ?? ''}"?`)) return;
    this.employeeService.deleteEmployee(employee.id).subscribe({
      next: () => this.loadEmployees(),
      error: (err) => {
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to delete employee.';
        console.error('Employee delete error:', err);
      }
    });
  }

  trackByEmployeeId(_index: number, employee: Employee): number {
    return employee.id;
  }
}
