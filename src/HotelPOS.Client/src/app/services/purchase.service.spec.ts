import { MOCK_PURCHASES, MOCK_SUPPLIERS } from '../testing/test-data';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { PurchaseService } from './purchase.service';
import { environment } from '../../environments/environment';
import { Purchase, SavePurchaseRequest } from '../models/purchase.model';
import { Supplier } from '../models/supplier.model';

describe('PurchaseService', () => {
  let service: PurchaseService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [PurchaseService, provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(PurchaseService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getPurchases', () => {
    it('should retrieve all purchases', () => {
      const dummyPurchases = MOCK_PURCHASES;

      service.getPurchases().subscribe(purchases => {
        expect(purchases).toEqual(dummyPurchases);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/purchases`);
      expect(req.request.method).toBe('GET');
      req.flush(dummyPurchases);
    });
  });

  describe('getSuppliers', () => {
    it('should retrieve suppliers scoped to purchases endpoint', () => {
      const dummySuppliers = MOCK_SUPPLIERS;

      service.getSuppliers().subscribe(suppliers => {
        expect(suppliers).toEqual(dummySuppliers);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/purchases/suppliers`);
      expect(req.request.method).toBe('GET');
      req.flush(dummySuppliers);
    });
  });

  describe('createPurchase', () => {
    it('should create a new purchase via POST', () => {
      const request: SavePurchaseRequest = {
        supplierId: 1,
        invoiceNumber: 'INV-002',
        purchaseDate: '2026-07-22',
        paymentType: 'Credit',
        totalDiscount: 0,
        items: [
          { itemId: 1, itemName: 'Item 1', quantity: 10, unitPrice: 50, taxPercentage: 5, discount: 0 }
        ]
      };
      const createdPurchase: Purchase = {
        sNo: 1, id: 2, supplierId: 1, invoiceNumber: 'INV-002', purchaseDate: '2026-07-22',
        paymentType: 'Credit', subtotal: 500, totalTax: 25, totalDiscount: 0, grandTotal: 525,
        items: [{ id: 1, itemId: 1, itemName: 'Item 1', quantity: 10, unitPrice: 50, taxPercentage: 5, discount: 0, total: 525 }]
      };

      service.createPurchase(request).subscribe(purchase => {
        expect(purchase).toEqual(createdPurchase);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/purchases`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(request);
      req.flush(createdPurchase);
    });
  });
});
