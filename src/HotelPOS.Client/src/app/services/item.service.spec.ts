import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ItemService } from './item.service';
import { environment } from '../../environments/environment';
import { Item } from '../models/item.model';

describe('ItemService', () => {
  let service: ItemService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ItemService, provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(ItemService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should retrieve items from the API via GET', () => {
    const dummyItems: Item[] = [
      { id: 1, name: 'Item 1', price: 100, taxPercentage: 5, stockQuantity: 10, trackInventory: true, unitId: 1 },
      { id: 2, name: 'Item 2', price: 200, taxPercentage: 12, stockQuantity: 5, trackInventory: false, unitId: 1 }
    ];

    service.getItems().subscribe(items => {
      expect(items).toHaveSize(2);
      expect(items).toEqual(dummyItems);
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/items`);
    expect(req.request.method).toBe('GET');
    req.flush(dummyItems);
  });

  it('should retrieve a single item from the API via GET', () => {
    const dummyItem: Item = { id: 1, name: 'Item 1', price: 100, taxPercentage: 5, stockQuantity: 10, trackInventory: true, unitId: 1 };

    service.getItem(1).subscribe(item => {
      expect(item).toEqual(dummyItem);
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/items/1`);
    expect(req.request.method).toBe('GET');
    req.flush(dummyItem);
  });

  it('should create an item via POST', () => {
    const itemToCreate: Partial<Item> = { name: 'New Item', price: 150 };
    const createdItem: Item = { id: 3, name: 'New Item', price: 150, taxPercentage: 5, stockQuantity: 0, trackInventory: false, unitId: 1 };

    service.createItem(itemToCreate).subscribe(item => {
      expect(item).toEqual(createdItem);
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/items`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(itemToCreate);
    req.flush(createdItem);
  });
});
