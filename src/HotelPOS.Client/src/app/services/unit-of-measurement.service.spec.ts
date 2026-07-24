import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { UnitOfMeasurementService } from './unit-of-measurement.service';
import { environment } from '../../environments/environment';
import { UnitOfMeasurement } from '../models/item.model';

describe('UnitOfMeasurementService', () => {
  let service: UnitOfMeasurementService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [UnitOfMeasurementService, provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(UnitOfMeasurementService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should retrieve units from the API via GET', () => {
    const dummyUnits: UnitOfMeasurement[] = [
      { id: 1, name: 'kg', displayOrder: 1 },
      { id: 2, name: 'litre', displayOrder: 2 }
    ];

    service.getUnits().subscribe(units => {
      expect(units).toEqual(dummyUnits);
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/unitofmeasurements`);
    expect(req.request.method).toBe('GET');
    req.flush(dummyUnits);
  });

  it('should create a unit via POST', () => {
    const unitToCreate: Partial<UnitOfMeasurement> = { name: 'Piece', displayOrder: 1 };
    const createdUnit: UnitOfMeasurement = { id: 3, name: 'Piece', displayOrder: 1 };

    service.createUnit(unitToCreate).subscribe(unit => {
      expect(unit).toEqual(createdUnit);
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/unitofmeasurements`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(unitToCreate);
    req.flush(createdUnit);
  });

  it('should update a unit via PUT', () => {
    const unitToUpdate: Partial<UnitOfMeasurement> = { name: 'Kilogram', displayOrder: 1 };

    service.updateUnit(1, unitToUpdate).subscribe();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/unitofmeasurements/1`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(unitToUpdate);
    req.flush(null);
  });

  it('should delete a unit via DELETE', () => {
    service.deleteUnit(1).subscribe();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/unitofmeasurements/1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
