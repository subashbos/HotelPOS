import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TableService } from './table.service';
import { environment } from '../../environments/environment';
import { DiningTable } from '../models/table.model';

describe('TableService', () => {
  let service: TableService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [TableService, provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(TableService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getTables', () => {
    it('should retrieve all tables', () => {
      const dummyTables: DiningTable[] = [
        { id: 1, number: 1, name: 'Table 1', capacity: 4, isActive: true, isDeleted: false },
        { id: 2, number: 2, name: 'Table 2', capacity: 2, isActive: true, isDeleted: false }
      ];

      service.getTables().subscribe(tables => {
        expect(tables).toHaveSize(2);
        expect(tables).toEqual(dummyTables);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/tables`);
      expect(req.request.method).toBe('GET');
      req.flush(dummyTables);
    });
  });

  describe('createTable', () => {
    it('should create a new table via POST', () => {
      const newTable: Partial<DiningTable> = { number: 3, name: 'Table 3', capacity: 6 };
      const createdTable: DiningTable = { id: 3, number: 3, name: 'Table 3', capacity: 6, isActive: true, isDeleted: false };

      service.createTable(newTable).subscribe(table => {
        expect(table).toEqual(createdTable);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/tables`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(newTable);
      req.flush(createdTable);
    });
  });

  describe('updateTable', () => {
    it('should update a table via PUT', () => {
      const updateRequest: Partial<DiningTable> = { name: 'Updated Table', capacity: 8 };

      service.updateTable(1, updateRequest).subscribe(response => {
        expect(response).toBeNull();
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/tables/1`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(updateRequest);
      req.flush(null);
    });
  });

  describe('deleteTable', () => {
    it('should delete a table via DELETE', () => {
      service.deleteTable(1).subscribe(response => {
        expect(response).toBeNull();
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/tables/1`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });
  });
});
