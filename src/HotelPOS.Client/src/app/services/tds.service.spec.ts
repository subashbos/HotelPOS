import { MOCK_TDS_RULESET, MOCK_FINANCIAL_YEARS } from '../testing/test-data';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TdsService } from './tds.service';
import { environment } from '../../environments/environment';
import { SaveTdsRuleSetRequest, TdsRuleSet } from '../models/tds.model';

describe('TdsService', () => {
  let service: TdsService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiBaseUrl}/tds`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [TdsService, provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(TdsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getFinancialYears', () => {
    it('should retrieve the list of financial years', () => {
      const years = MOCK_FINANCIAL_YEARS;

      service.getFinancialYears().subscribe(result => {
        expect(result).toEqual(years);
      });

      const req = httpMock.expectOne(`${apiUrl}/financial-years`);
      expect(req.request.method).toBe('GET');
      req.flush(years);
    });
  });

  describe('getRuleSet', () => {
    it('should retrieve a rule set for a financial year', () => {
      const ruleSet = MOCK_TDS_RULESET;

      service.getRuleSet(2025).subscribe(result => {
        expect(result).toEqual(ruleSet);
      });

      const req = httpMock.expectOne(`${apiUrl}/rule-sets/2025`);
      expect(req.request.method).toBe('GET');
      req.flush(ruleSet);
    });
  });

  describe('saveRuleSet', () => {
    it('should save a rule set via PUT', () => {
      const request: SaveTdsRuleSetRequest = {
        standardDeduction: 75000,
        rebateIncomeLimit: 700000,
        cessRatePercent: 4,
        slabs: [{ incomeFrom: 0, incomeTo: null, ratePercent: 5, displayOrder: 1 }]
      };

      service.saveRuleSet(2025, request).subscribe(response => {
        expect(response).toBeNull();
      });

      const req = httpMock.expectOne(`${apiUrl}/rule-sets/2025`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(request);
      req.flush(null);
    });
  });

  describe('deleteRuleSet', () => {
    it('should delete a rule set via DELETE', () => {
      service.deleteRuleSet(2025).subscribe(response => {
        expect(response).toBeNull();
      });

      const req = httpMock.expectOne(`${apiUrl}/rule-sets/2025`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });
  });
});
