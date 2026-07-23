import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { EmployeesComponent } from './employees.component';
import { EmployeeService } from '../../../services/employee.service';
import { Employee } from '../../../models/employee.model';

describe('EmployeesComponent', () => {
  let component: EmployeesComponent;
  let fixture: ComponentFixture<EmployeesComponent>;
  let employeeServiceSpy: jasmine.SpyObj<EmployeeService>;

  const mockEmployee: Employee = {
    id: 1,
    employeeCode: 'EMP001',
    firstName: 'John',
    lastName: 'Doe',
    gender: 'Male',
    dateOfJoining: '2026-01-01',
    employmentType: 'FullTime',
    status: 'Active'
  };

  beforeEach(async () => {
    employeeServiceSpy = jasmine.createSpyObj('EmployeeService', [
      'getEmployees',
      'getDepartments',
      'getDesignations',
      'createEmployee',
      'updateEmployee',
      'deleteEmployee'
    ]);

    employeeServiceSpy.getEmployees.and.returnValue(of([mockEmployee]));
    employeeServiceSpy.getDepartments.and.returnValue(of([{ id: 1, name: 'IT' }]));
    employeeServiceSpy.getDesignations.and.returnValue(of([{ id: 1, title: 'Dev', departmentId: 1 }]));

    await TestBed.configureTestingModule({
      declarations: [EmployeesComponent],
      imports: [FormsModule],
      providers: [
        { provide: EmployeeService, useValue: employeeServiceSpy }
      ]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(EmployeesComponent);
    component = fixture.componentInstance;
  });

  it('should create component and load employees, departments, and designations', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
    expect(employeeServiceSpy.getEmployees).toHaveBeenCalled();
    expect(component.employees).toHaveSize(1);
    expect(component.departments).toHaveSize(1);
    expect(component.designations).toHaveSize(1);
  });

  it('should handle employees load error', () => {
    spyOn(console, 'error');
    employeeServiceSpy.getEmployees.and.returnValue(throwError(() => new Error('Load failed')));
    fixture.detectChanges();
    expect(component.isLoading).toBeFalse();
    expect(component.loadError).toBe('Failed to load employees. Please check the server connection.');
  });

  it('should open add form and close form', () => {
    component.openAddForm();
    expect(component.showForm).toBeTrue();
    expect(component.editingId).toBeNull();

    component.closeForm();
    expect(component.showForm).toBeFalse();
  });

  it('should open edit form with employee data', () => {
    component.openEditForm(mockEmployee);
    expect(component.showForm).toBeTrue();
    expect(component.editingId).toBe(1);
    expect(component.form.firstName).toBe('John');
  });

  it('should save new employee', () => {
    employeeServiceSpy.createEmployee.and.returnValue(of(mockEmployee));
    component.openAddForm();
    component.form.firstName = 'Jane';
    component.form.dateOfJoining = '2026-01-01';

    component.save();

    expect(employeeServiceSpy.createEmployee).toHaveBeenCalled();
    expect(component.showForm).toBeFalse();
  });

  it('should update existing employee', () => {
    employeeServiceSpy.updateEmployee.and.returnValue(of(void 0));
    component.openEditForm(mockEmployee);
    component.form.firstName = 'John Updated';

    component.save();

    expect(employeeServiceSpy.updateEmployee).toHaveBeenCalled();
    expect(component.showForm).toBeFalse();
  });

  it('should delete employee when confirmed', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    employeeServiceSpy.deleteEmployee.and.returnValue(of(void 0));

    component.deleteEmployee(mockEmployee);

    expect(employeeServiceSpy.deleteEmployee).toHaveBeenCalledWith(1);
  });

  it('should filter designations by departmentId', () => {
    component.designations = [
      { id: 1, title: 'Dev', departmentId: 1 },
      { id: 2, title: 'HR Mgr', departmentId: 2 }
    ];
    expect(component.designationsFor(1)).toHaveSize(1);
    expect(component.designationsFor(undefined)).toHaveSize(2);
  });

  it('should track employee by id', () => {
    expect(component.trackByEmployeeId(0, mockEmployee)).toBe(1);
  });
});
