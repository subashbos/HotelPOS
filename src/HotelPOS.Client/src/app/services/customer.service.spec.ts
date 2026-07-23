import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { CustomerService } from './customer.service';
import { environment } from '../../environments/environment';
import { Customer, CustomerHistory, SaveCustomerRequest } from '../models/customer.model';

describe('CustomerService', () => {
  let service: CustomerService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [CustomerService, provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(CustomerService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getCustomers', () => {
    it('should retrieve all customers with default includeInactive=false', () => {
      const dummyCustomers: Customer[] = [
        { id: 1, name: 'John Doe', email: 'john@example.com', phone: '1234567890', isActive: true, createdAt: '2026-01-01' },
        { id: 2, name: 'Jane Smith', email: 'jane@example.com', phone: '0987654321', isActive: true, createdAt: '2026-01-02' }
      ];

      service.getCustomers().subscribe(customers => {
        expect(customers).toHaveSize(2);
        expect(customers).toEqual(dummyCustomers);
      });

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/customers`);
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('includeInactive')).toBe('false');
      req.flush(dummyCustomers);
    });

    it('should retrieve all customers including inactive when includeInactive=true', () => {
      const dummyCustomers: Customer[] = [
        { id: 1, name: 'John Doe', email: 'john@example.com', phone: '1234567890', isActive: true, createdAt: '2026-01-01' },
        { id: 2, name: 'Inactive Customer', email: 'inactive@example.com', phone: '1111111111', isActive: false, createdAt: '2026-01-01' }
      ];

      service.getCustomers(true).subscribe(customers => {
        expect(customers).toHaveSize(2);
      });

      const req = httpMock.expectOne(req => req.url === `${environment.apiBaseUrl}/customers`);
      expect(req.request.params.get('includeInactive')).toBe('true');
      req.flush(dummyCustomers);
    });
  });

  describe('getCustomer', () => {
    it('should retrieve a single customer by id', () => {
      const dummyCustomer: Customer = {
        id: 1,
        name: 'John Doe',
        email: 'john@example.com',
        phone: '1234567890',
        isActive: true,
        createdAt: '2026-01-01'
      };

      service.getCustomer(1).subscribe(customer => {
        expect(customer).toEqual(dummyCustomer);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/customers/1`);
      expect(req.request.method).toBe('GET');
      req.flush(dummyCustomer);
    });
  });

  describe('getCustomerHistory', () => {
    it('should retrieve customer history by id', () => {
      const dummyHistory: CustomerHistory = {
        customerId: 1,
        customerName: 'John Doe',
        totalOrders: 10,
        totalSpent: 5000,
        orders: []
      };

      service.getCustomerHistory(1).subscribe(history => {
        expect(history).toEqual(dummyHistory);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/customers/1/history`);
      expect(req.request.method).toBe('GET');
      req.flush(dummyHistory);
    });
  });

  describe('createCustomer', () => {
    it('should create a new customer via POST', () => {
      const newCustomerRequest: SaveCustomerRequest = {
        id: 0,
        name: 'New Customer',
        email: 'new@example.com',
        phone: '5555555555'
      };
      const createdCustomer: Customer = {
        id: 3,
        name: 'New Customer',
        email: 'new@example.com',
        phone: '5555555555',
        isActive: true,
        createdAt: '2026-07-22'
      };

      service.createCustomer(newCustomerRequest).subscribe(customer => {
        expect(customer).toEqual(createdCustomer);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/customers`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(newCustomerRequest);
      req.flush(createdCustomer);
    });
  });

  describe('updateCustomer', () => {
    it('should update a customer via PUT', () => {
      const updateRequest: SaveCustomerRequest = {
        id: 1,
        name: 'Updated Name',
        email: 'updated@example.com',
        phone: '9999999999'
      };

      service.updateCustomer(1, updateRequest).subscribe(response => {
        expect(response).toBeNull();
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/customers/1`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(updateRequest);
      req.flush(null);
    });
  });

  describe('deleteCustomer', () => {
    it('should delete a customer via DELETE', () => {
      service.deleteCustomer(1).subscribe(response => {
        expect(response).toBeNull();
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/customers/1`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });
  });
});
