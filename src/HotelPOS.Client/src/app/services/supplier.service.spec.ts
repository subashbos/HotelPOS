import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { SupplierService } from './supplier.service';
import { environment } from '../../environments/environment';
import { Supplier } from '../models/supplier.model';

describe('SupplierService', () => {
  let service: SupplierService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [SupplierService, provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(SupplierService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getSuppliers', () => {
    it('should retrieve all suppliers', () => {
      const dummySuppliers: Supplier[] = [
        { id: 1, name: 'Supplier A', contactPerson: 'Alice', phone: '1234567890', email: 'a@example.com', openingBalance: 0, creditLimit: 10000 },
        { id: 2, name: 'Supplier B', contactPerson: 'Bob', phone: '0987654321', email: 'b@example.com', openingBalance: 500, creditLimit: 20000 }
      ];

      service.getSuppliers().subscribe(suppliers => {
        expect(suppliers).toHaveSize(2);
        expect(suppliers).toEqual(dummySuppliers);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/suppliers`);
      expect(req.request.method).toBe('GET');
      req.flush(dummySuppliers);
    });
  });

  describe('getSupplier', () => {
    it('should retrieve a single supplier by id', () => {
      const dummySupplier: Supplier = {
        id: 1,
        name: 'Supplier A',
        contactPerson: 'Alice',
        phone: '1234567890',
        email: 'a@example.com',
        openingBalance: 0,
        creditLimit: 10000
      };

      service.getSupplier(1).subscribe(supplier => {
        expect(supplier).toEqual(dummySupplier);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/suppliers/1`);
      expect(req.request.method).toBe('GET');
      req.flush(dummySupplier);
    });
  });

  describe('createSupplier', () => {
    it('should create a new supplier via POST', () => {
      const newSupplier: Partial<Supplier> = {
        name: 'New Supplier',
        contactPerson: 'Charlie',
        phone: '5555555555',
        email: 'new@example.com'
      };
      const createdSupplier: Supplier = { id: 3, ...newSupplier } as Supplier;

      service.createSupplier(newSupplier).subscribe(supplier => {
        expect(supplier).toEqual(createdSupplier);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/suppliers`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(newSupplier);
      req.flush(createdSupplier);
    });
  });

  describe('updateSupplier', () => {
    it('should update a supplier via PUT', () => {
      const updateRequest: Partial<Supplier> = { name: 'Updated Supplier' };

      service.updateSupplier(1, updateRequest).subscribe(response => {
        expect(response).toBeNull();
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/suppliers/1`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(updateRequest);
      req.flush(null);
    });
  });

  describe('deleteSupplier', () => {
    it('should delete a supplier via DELETE', () => {
      service.deleteSupplier(1).subscribe(response => {
        expect(response).toBeNull();
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/suppliers/1`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });
  });
});
