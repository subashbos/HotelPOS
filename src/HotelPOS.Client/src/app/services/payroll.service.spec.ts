import { MOCK_SALARY_STRUCTURES, MOCK_PAYROLL_RUN, MOCK_PAYSLIPS } from '../testing/test-data';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { PayrollService } from './payroll.service';
import { environment } from '../../environments/environment';
import { PayrollRun, Payslip, SalaryStructure, SaveSalaryStructureRequest } from '../models/payroll.model';

describe('PayrollService', () => {
  let service: PayrollService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [PayrollService, provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(PayrollService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getSalaryStructures', () => {
    it('should retrieve salary structures for an employee', () => {
      const dummyStructures = MOCK_SALARY_STRUCTURES;

      service.getSalaryStructures(5).subscribe(structures => {
        expect(structures).toEqual(dummyStructures);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/payroll/salary-structures/5`);
      expect(req.request.method).toBe('GET');
      req.flush(dummyStructures);
    });
  });

  describe('saveSalaryStructure', () => {
    it('should save a salary structure via POST', () => {
      const request: SaveSalaryStructureRequest = {
        id: 0, employeeId: 5, effectiveFrom: '2026-01-01', basic: 20000, hra: 8000, da: 2000,
        conveyanceAllowance: 1000, medicalAllowance: 1000, specialAllowance: 500,
        pfApplicable: true, esiApplicable: false, professionalTaxApplicable: true
      };
      const savedStructure: SalaryStructure = { ...request, id: 1, grossMonthly: 32500 };

      service.saveSalaryStructure(request).subscribe(structure => {
        expect(structure).toEqual(savedStructure);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/payroll/salary-structures`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(request);
      req.flush(savedStructure);
    });
  });

  describe('runPayroll', () => {
    it('should run payroll for a given month and year via POST', () => {
      const dummyRun = MOCK_PAYROLL_RUN;

      service.runPayroll(7, 2026).subscribe(run => {
        expect(run).toEqual(dummyRun);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/payroll/run`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ month: 7, year: 2026 });
      req.flush(dummyRun);
    });
  });

  describe('markRunAsPaid', () => {
    it('should mark a payroll run as paid via POST', () => {
      service.markRunAsPaid(1).subscribe(response => {
        expect(response).toBeNull();
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/payroll/runs/1/mark-paid`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toBeNull();
      req.flush(null);
    });
  });

  describe('getRuns', () => {
    it('should retrieve all payroll runs', () => {
      const dummyRuns = [MOCK_PAYROLL_RUN];

      service.getRuns().subscribe(runs => {
        expect(runs).toEqual(dummyRuns);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/payroll/runs`);
      expect(req.request.method).toBe('GET');
      req.flush(dummyRuns);
    });
  });

  describe('getRun', () => {
    it('should retrieve a single payroll run by id', () => {
      const dummyRun: PayrollRun = { id: 1, month: 7, year: 2026, status: 'Processed', payslips: [] };

      service.getRun(1).subscribe(run => {
        expect(run).toEqual(dummyRun);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/payroll/runs/1`);
      expect(req.request.method).toBe('GET');
      req.flush(dummyRun);
    });
  });

  describe('getPayslips', () => {
    it('should retrieve payslips for an employee', () => {
      const dummyPayslips = MOCK_PAYSLIPS;

      service.getPayslips(5).subscribe(payslips => {
        expect(payslips).toEqual(dummyPayslips);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/payroll/payslips/5`);
      expect(req.request.method).toBe('GET');
      req.flush(dummyPayslips);
    });
  });
});
