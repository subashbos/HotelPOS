import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { EmployeeService } from './employee.service';
import { environment } from '../../environments/environment';
import { Employee, SaveEmployeeRequest, Department, Designation } from '../models/employee.model';

describe('EmployeeService', () => {
  let service: EmployeeService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [EmployeeService, provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(EmployeeService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getEmployees', () => {
    it('should retrieve all employees', () => {
      const dummyEmployees: Employee[] = [
        {
          id: 1,
          employeeCode: 'EMP001',
          firstName: 'John',
          lastName: 'Doe',
          email: 'john@example.com',
          phone: '1234567890',
          departmentId: 1,
          designationId: 1,
          dateOfJoining: '2025-01-01',
          employmentType: 'Permanent',
          status: 'Active'
        },
        {
          id: 2,
          employeeCode: 'EMP002',
          firstName: 'Jane',
          lastName: 'Smith',
          email: 'jane@example.com',
          phone: '0987654321',
          departmentId: 1,
          designationId: 2,
          dateOfJoining: '2025-02-01',
          employmentType: 'Permanent',
          status: 'Active'
        }
      ];

      service.getEmployees().subscribe(employees => {
        expect(employees).toHaveSize(2);
        expect(employees).toEqual(dummyEmployees);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/employees`);
      expect(req.request.method).toBe('GET');
      req.flush(dummyEmployees);
    });
  });

  describe('getEmployee', () => {
    it('should retrieve a single employee by id', () => {
      const dummyEmployee: Employee = {
        id: 1,
        employeeCode: 'EMP001',
        firstName: 'John',
        lastName: 'Doe',
        email: 'john@example.com',
        phone: '1234567890',
        departmentId: 1,
        designationId: 1,
        dateOfJoining: '2025-01-01',
        employmentType: 'Permanent',
        status: 'Active'
      };

      service.getEmployee(1).subscribe(employee => {
        expect(employee).toEqual(dummyEmployee);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/employees/1`);
      expect(req.request.method).toBe('GET');
      req.flush(dummyEmployee);
    });
  });

  describe('getDepartments', () => {
    it('should retrieve all departments', () => {
      const dummyDepartments: Department[] = [
        { id: 1, name: 'Kitchen' },
        { id: 2, name: 'Front Office' },
        { id: 3, name: 'Management' }
      ];

      service.getDepartments().subscribe(departments => {
        expect(departments).toHaveSize(3);
        expect(departments).toEqual(dummyDepartments);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/employees/departments`);
      expect(req.request.method).toBe('GET');
      req.flush(dummyDepartments);
    });
  });

  describe('getDesignations', () => {
    it('should retrieve all designations', () => {
      const dummyDesignations: Designation[] = [
        { id: 1, title: 'Chef', departmentId: 1 },
        { id: 2, title: 'Waiter', departmentId: 2 },
        { id: 3, title: 'Manager', departmentId: 3 }
      ];

      service.getDesignations().subscribe(designations => {
        expect(designations).toHaveSize(3);
        expect(designations).toEqual(dummyDesignations);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/employees/designations`);
      expect(req.request.method).toBe('GET');
      req.flush(dummyDesignations);
    });
  });

  describe('createEmployee', () => {
    it('should create a new employee via POST', () => {
      const newEmployeeRequest: SaveEmployeeRequest = {
        id: 0,
        firstName: 'New',
        lastName: 'Employee',
        email: 'new@example.com',
        phone: '5555555555',
        departmentId: 1,
        designationId: 2,
        dateOfJoining: '2026-07-22',
        employmentType: 'Permanent',
        status: 'Active'
      };
      const createdEmployee: Employee = {
        ...newEmployeeRequest,
        id: 3,
        employeeCode: 'EMP003'
      };

      service.createEmployee(newEmployeeRequest).subscribe(employee => {
        expect(employee).toEqual(createdEmployee);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/employees`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(newEmployeeRequest);
      req.flush(createdEmployee);
    });
  });

  describe('updateEmployee', () => {
    it('should update an employee via PUT', () => {
      const updateRequest: SaveEmployeeRequest = {
        id: 1,
        firstName: 'Updated',
        lastName: 'Name',
        email: 'updated@example.com',
        phone: '9999999999',
        departmentId: 2,
        designationId: 1,
        dateOfJoining: '2025-01-01',
        employmentType: 'Permanent',
        status: 'Active'
      };

      service.updateEmployee(1, updateRequest).subscribe(response => {
        expect(response).toBeNull();
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/employees/1`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(updateRequest);
      req.flush(null);
    });
  });

  describe('deleteEmployee', () => {
    it('should delete an employee via DELETE', () => {
      service.deleteEmployee(1).subscribe(response => {
        expect(response).toBeNull();
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/employees/1`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });
  });
});
