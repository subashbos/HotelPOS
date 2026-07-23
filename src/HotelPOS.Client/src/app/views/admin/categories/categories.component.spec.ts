import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { CategoriesComponent } from './categories.component';
import { CategoryService } from '../../../services/category.service';
import { Category } from '../../../models/item.model';

describe('CategoriesComponent', () => {
  let component: CategoriesComponent;
  let fixture: ComponentFixture<CategoriesComponent>;
  let categoryServiceSpy: jasmine.SpyObj<CategoryService>;

  const mockCategory: Category = {
    id: 1,
    name: 'Beverages',
    displayOrder: 1
  };

  beforeEach(async () => {
    categoryServiceSpy = jasmine.createSpyObj('CategoryService', [
      'getCategories',
      'createCategory',
      'updateCategory',
      'deleteCategory'
    ]);
    categoryServiceSpy.getCategories.and.returnValue(of([mockCategory]));

    await TestBed.configureTestingModule({
      declarations: [CategoriesComponent],
      imports: [FormsModule],
      providers: [
        { provide: CategoryService, useValue: categoryServiceSpy }
      ]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(CategoriesComponent);
    component = fixture.componentInstance;
  });

  it('should create component and load categories', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
    expect(categoryServiceSpy.getCategories).toHaveBeenCalled();
    expect().toHaveSize();
  });

  it('should handle load error', () => {
    spyOn(console, 'error');
    categoryServiceSpy.getCategories.and.returnValue(throwError(() => new Error('Error loading')));
    fixture.detectChanges();
    expect(component.isLoading).toBeFalse();
    expect(component.loadError).toBe('Failed to load categories. Please check the server connection.');
  });

  it('should add new category', () => {
    categoryServiceSpy.createCategory.and.returnValue(of(mockCategory));
    component.newName = 'Snacks';
    component.newDisplayOrder = 2;

    component.addCategory();

    expect(categoryServiceSpy.createCategory).toHaveBeenCalledWith({ name: 'Snacks', displayOrder: 2 });
    expect(component.newName).toBe('');
  });

  it('should start, cancel, and save edit', () => {
    categoryServiceSpy.updateCategory.and.returnValue(of(void 0));
    component.startEdit(mockCategory);
    expect(component.editingId).toBe(1);
    expect(component.editName).toBe('Beverages');

    component.cancelEdit();
    expect(component.editingId).toBeNull();

    component.startEdit(mockCategory);
    component.editName = 'Cold Drinks';
    component.saveEdit(mockCategory);

    expect(categoryServiceSpy.updateCategory).toHaveBeenCalledWith(1, { name: 'Cold Drinks', displayOrder: 1 });
    expect(component.editingId).toBeNull();
  });

  it('should delete category when confirmed', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    categoryServiceSpy.deleteCategory.and.returnValue(of(void 0));

    component.deleteCategory(mockCategory);

    expect(categoryServiceSpy.deleteCategory).toHaveBeenCalledWith(1);
  });

  it('should track category by id', () => {
    expect(component.trackByCategoryId(0, mockCategory)).toBe(1);
  });

  it('should not add category if newName is empty or whitespace or component isSaving', () => {
    component.newName = '   ';
    component.addCategory();
    expect(categoryServiceSpy.createCategory).not.toHaveBeenCalled();

    component.newName = 'Valid';
    component.isSaving = true;
    component.addCategory();
    expect(categoryServiceSpy.createCategory).not.toHaveBeenCalled();
  });

  it('should handle addCategory error payload variations', () => {
    spyOn(console, 'error');
    component.newName = 'New Cat';

    // err.error.message
    categoryServiceSpy.createCategory.and.returnValue(throwError(() => ({ error: { message: 'Msg1' } })));
    component.addCategory();
    expect(component.saveError).toBe('Msg1');

    // err.error.Message
    component.newName = 'New Cat';
    categoryServiceSpy.createCategory.and.returnValue(throwError(() => ({ error: { Message: 'Msg2' } })));
    component.addCategory();
    expect(component.saveError).toBe('Msg2');

    // string err.error
    component.newName = 'New Cat';
    categoryServiceSpy.createCategory.and.returnValue(throwError(() => ({ error: 'Msg3' })));
    component.addCategory();
    expect(component.saveError).toBe('Msg3');

    // default fallback
    component.newName = 'New Cat';
    categoryServiceSpy.createCategory.and.returnValue(throwError(() => ({})));
    component.addCategory();
    expect(component.saveError).toBe('Failed to create category.');
  });

  it('should not save edit if editName is empty and should handle saveEdit error', () => {
    spyOn(console, 'error');
    component.startEdit(mockCategory);
    component.editName = '   ';
    component.saveEdit(mockCategory);
    expect(categoryServiceSpy.updateCategory).not.toHaveBeenCalled();

    component.editName = 'Updated';
    categoryServiceSpy.updateCategory.and.returnValue(throwError(() => ({ error: { message: 'Update error' } })));
    component.saveEdit(mockCategory);
    expect(component.saveError).toBe('Update error');
  });

  it('should not delete category when confirm returns false and handle delete error', () => {
    spyOn(console, 'error');
    const confirmSpy = spyOn(window, 'confirm');
    confirmSpy.and.returnValue(false);
    component.deleteCategory(mockCategory);
    expect(categoryServiceSpy.deleteCategory).not.toHaveBeenCalled();

    confirmSpy.and.returnValue(true);
    categoryServiceSpy.deleteCategory.and.returnValue(throwError(() => ({ error: { message: 'Delete error' } })));
    component.deleteCategory(mockCategory);
    expect(component.saveError).toBe('Delete error');
  });

  it('should sort loaded categories by displayOrder', () => {
    const list: Category[] = [
      { id: 1, name: 'B', displayOrder: 20 },
      { id: 2, name: 'A', displayOrder: 5 },
      { id: 3, name: 'C', displayOrder: 10 }
    ];
    categoryServiceSpy.getCategories.and.returnValue(of(list));
    component.loadCategories();
    expect(component.categories.map(c => c.id)).toEqual([2, 3, 1]);
  });
});
