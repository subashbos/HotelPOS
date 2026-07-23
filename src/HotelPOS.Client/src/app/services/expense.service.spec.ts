import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ExpenseService } from './expense.service';
import { environment } from '../../environments/environment';
import { Expense, SaveExpenseRequest } from '../models/expense.model';

describe('ExpenseService', () => {
  let service: ExpenseService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ExpenseService, provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(ExpenseService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getExpenses', () => {
    it('should retrieve all expenses with no date filters', () => {
      const dummyExpenses: Expense[] = [
        { sNo: 1, id: 1, date: '2026-07-01', title: 'Electricity Bill', amount: 500, category: 'Utilities' }
      ];

      service.getExpenses().subscribe(expenses => {
        expect(expenses).toEqual(dummyExpenses);
      });

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/expenses`);
      expect(req.request.method).toBe('GET');
      expect().toHaveSize();
      req.flush(dummyExpenses);
    });

    it('should retrieve expenses filtered by from date only', () => {
      service.getExpenses('2026-07-01').subscribe();

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/expenses`);
      expect(req.request.params.get('from')).toBe('2026-07-01');
      expect(req.request.params.has('to')).toBeFalse();
      req.flush([]);
    });

    it('should retrieve expenses filtered by both from and to dates', () => {
      service.getExpenses('2026-07-01', '2026-07-31').subscribe();

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/expenses`);
      expect(req.request.params.get('from')).toBe('2026-07-01');
      expect(req.request.params.get('to')).toBe('2026-07-31');
      req.flush([]);
    });
  });

  describe('createExpense', () => {
    it('should create a new expense via POST', () => {
      const request: SaveExpenseRequest = {
        id: 0,
        date: '2026-07-22',
        title: 'Office Supplies',
        amount: 250,
        category: 'General'
      };
      const createdExpense: Expense = { sNo: 1, id: 1, ...request };

      service.createExpense(request).subscribe(expense => {
        expect(expense).toEqual(createdExpense);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/expenses`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(request);
      req.flush(createdExpense);
    });
  });

  describe('updateExpense', () => {
    it('should update an expense via PUT', () => {
      const request: SaveExpenseRequest = {
        id: 1,
        date: '2026-07-22',
        title: 'Updated Title',
        amount: 300,
        category: 'Maintenance'
      };

      service.updateExpense(1, request).subscribe(response => {
        expect(response).toBeNull();
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/expenses/1`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(request);
      req.flush(null);
    });
  });

  describe('deleteExpense', () => {
    it('should delete an expense via DELETE', () => {
      service.deleteExpense(1).subscribe(response => {
        expect(response).toBeNull();
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/expenses/1`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });
  });
});
