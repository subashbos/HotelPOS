import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { BomService } from './bom.service';
import { environment } from '../../environments/environment';
import { BomIngredientRow, MenuItemBom } from '../models/bom.model';

describe('BomService', () => {
  let service: BomService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiBaseUrl}/bom`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [BomService, provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(BomService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getBomForMenuItem', () => {
    it('should retrieve the BOM for a menu item', () => {
      const bom: MenuItemBom = {
        menuItemId: 5,
        menuItemName: 'Butter Chicken',
        menuPrice: 350,
        ingredients: [],
        totalNetCost: 0,
        totalWastageCost: 0,
        totalFoodCost: 0,
        grossMargin: 350
      };

      service.getBomForMenuItem(5).subscribe(result => {
        expect(result).toEqual(bom);
      });

      const req = httpMock.expectOne(`${apiUrl}/menu-item/5`);
      expect(req.request.method).toBe('GET');
      req.flush(bom);
    });
  });

  describe('saveBom', () => {
    it('should save ingredients via POST wrapped in an ingredients object', () => {
      const ingredients: BomIngredientRow[] = [
        {
          rawMaterialId: 1,
          rawMaterialName: 'Chicken',
          unit: 'kg',
          quantityRequired: 0.25,
          wastagePercentage: 5,
          effectiveQuantity: 0.2625,
          costPerUnit: 220,
          wastageCost: 2.75,
          ingredientCost: 57.75
        }
      ];
      const bom: MenuItemBom = {
        menuItemId: 5,
        menuItemName: 'Butter Chicken',
        menuPrice: 350,
        ingredients,
        totalNetCost: 55,
        totalWastageCost: 2.75,
        totalFoodCost: 57.75,
        grossMargin: 292.25
      };

      service.saveBom(5, ingredients).subscribe(result => {
        expect(result).toEqual(bom);
      });

      const req = httpMock.expectOne(`${apiUrl}/menu-item/5`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ ingredients });
      req.flush(bom);
    });
  });

  describe('deleteBom', () => {
    it('should delete the BOM for a menu item via DELETE', () => {
      service.deleteBom(5).subscribe(response => {
        expect(response).toBeNull();
      });

      const req = httpMock.expectOne(`${apiUrl}/menu-item/5`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });
  });
});
