import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { RawMaterialService } from './raw-material.service';
import { environment } from '../../environments/environment';
import { RawMaterial } from '../models/raw-material.model';

describe('RawMaterialService', () => {
  let service: RawMaterialService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiBaseUrl}/rawmaterials`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [RawMaterialService, provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(RawMaterialService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getRawMaterials', () => {
    it('should retrieve raw materials from the API', () => {
      const materials: RawMaterial[] = [
        { id: 1, name: 'Basmati Rice', unit: 'kg', costPerUnit: 110, currentStock: 50.5, minStockThreshold: 10 }
      ];

      service.getRawMaterials().subscribe(result => {
        expect(result).toEqual(materials);
      });

      const req = httpMock.expectOne(apiUrl);
      expect(req.request.method).toBe('GET');
      req.flush(materials);
    });

    it('should fall back to mock data when the API errors', () => {
      service.getRawMaterials().subscribe(result => {
        expect(result.length).toBeGreaterThan(0);
        expect(result[0].name).toBe('Basmati Rice');
      });

      const req = httpMock.expectOne(apiUrl);
      req.error(new ProgressEvent('error'));
    });
  });

  describe('createRawMaterial', () => {
    it('should create a raw material via POST', () => {
      const newMaterial: Partial<RawMaterial> = { name: 'Ghee', unit: 'kg', costPerUnit: 600 };
      const created: RawMaterial = { id: 9, name: 'Ghee', unit: 'kg', costPerUnit: 600, currentStock: 0, minStockThreshold: 0 };

      service.createRawMaterial(newMaterial).subscribe(result => {
        expect(result).toEqual(created);
      });

      const req = httpMock.expectOne(apiUrl);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(newMaterial);
      req.flush(created);
    });
  });

  describe('updateRawMaterial', () => {
    it('should update a raw material via PUT', () => {
      const update: Partial<RawMaterial> = { currentStock: 42 };
      const updated: RawMaterial = { id: 1, name: 'Basmati Rice', unit: 'kg', costPerUnit: 110, currentStock: 42, minStockThreshold: 10 };

      service.updateRawMaterial(1, update).subscribe(result => {
        expect(result).toEqual(updated);
      });

      const req = httpMock.expectOne(`${apiUrl}/1`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(update);
      req.flush(updated);
    });
  });

  describe('deleteRawMaterial', () => {
    it('should delete a raw material via DELETE', () => {
      service.deleteRawMaterial(1).subscribe(response => {
        expect(response).toBeNull();
      });

      const req = httpMock.expectOne(`${apiUrl}/1`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });
  });

  describe('getMockRawMaterials', () => {
    it('should emit the built-in mock list without an HTTP call', done => {
      service.getMockRawMaterials().subscribe(result => {
        expect(result.length).toBeGreaterThan(0);
        done();
      });

      httpMock.expectNone(apiUrl);
    });
  });
});
