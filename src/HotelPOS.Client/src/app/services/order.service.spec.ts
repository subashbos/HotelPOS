import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { OrderService } from './order.service';
import { environment } from '../../environments/environment';
import { CreateOrderRequest } from '../models/order.model';

describe('OrderService', () => {
  let service: OrderService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [OrderService, provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(OrderService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should create an order via POST and return the order id', () => {
    const request: CreateOrderRequest = {
      orderType: 'DineIn',
      tableNumber: 5,
      discount: 0,
      paymentMode: 'Cash',
      items: [{ itemId: 1, itemName: 'Chicken Curry', quantity: 2, price: 100, taxPercentage: 5, total: 210 }]
    };

    service.createOrder(request).subscribe(orderId => {
      expect(orderId).toBe(42);
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/orders`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(42);
  });
});
