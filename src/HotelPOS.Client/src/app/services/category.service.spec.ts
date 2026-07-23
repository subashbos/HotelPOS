import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { CategoryService } from './category.service';
import { environment } from '../../environments/environment';
import { Category } from '../models/item.model';

describe('CategoryService', () => {
  let service: CategoryService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [CategoryService, provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(CategoryService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getCategories', () => {
    it('should retrieve all categories', () => {
      const dummyCategories: Category[] = [
        { id: 1, name: 'Beverages', displayOrder: 1 },
        { id: 2, name: 'Food', displayOrder: 2 }
      ];

      service.getCategories().subscribe(categories => {
        expect(categories).toHaveSize(2);
        expect(categories).toEqual(dummyCategories);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/categories`);
      expect(req.request.method).toBe('GET');
      req.flush(dummyCategories);
    });
  });

  describe('createCategory', () => {
    it('should create a new category via POST', () => {
      const newCategory: Partial<Category> = { name: 'Desserts' };
      const createdCategory: Category = { id: 3, name: 'Desserts', displayOrder: 3 };

      service.createCategory(newCategory).subscribe(category => {
        expect(category).toEqual(createdCategory);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/categories`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(newCategory);
      req.flush(createdCategory);
    });
  });

  describe('updateCategory', () => {
    it('should update a category via PUT', () => {
      const updateRequest: Partial<Category> = { name: 'Updated Category' };

      service.updateCategory(1, updateRequest).subscribe(response => {
        expect(response).toBeNull();
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/categories/1`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(updateRequest);
      req.flush(null);
    });
  });

  describe('deleteCategory', () => {
    it('should delete a category via DELETE', () => {
      service.deleteCategory(1).subscribe(response => {
        expect(response).toBeNull();
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/categories/1`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });
  });
});
