import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { ItemsComponent } from './items.component';
import { ItemService } from '../../../services/item.service';
import { CategoryService } from '../../../services/category.service';
import { UnitOfMeasurementService } from '../../../services/unit-of-measurement.service';
import { Item, Category, UnitOfMeasurement } from '../../../models/item.model';

describe('ItemsComponent', () => {
  let component: ItemsComponent;
  let fixture: ComponentFixture<ItemsComponent>;
  let itemServiceSpy: jasmine.SpyObj<ItemService>;
  let categoryServiceSpy: jasmine.SpyObj<CategoryService>;
  let unitServiceSpy: jasmine.SpyObj<UnitOfMeasurementService>;

  const mockItems: Item[] = [
    { id: 1, name: 'Burger', price: 150, taxPercentage: 5, categoryId: 1, stockQuantity: 10, trackInventory: false, unitId: 1 }
  ];
  const mockCategories: Category[] = [
    { id: 1, name: 'Main Course', displayOrder: 1 }
  ];
  const mockUnits: UnitOfMeasurement[] = [
    { id: 1, name: 'Pcs', displayOrder: 0 }
  ];

  beforeEach(async () => {
    itemServiceSpy = jasmine.createSpyObj('ItemService', [
      'getItems',
      'createItem',
      'updateItem',
      'deleteItem'
    ]);
    categoryServiceSpy = jasmine.createSpyObj('CategoryService', ['getCategories']);
    unitServiceSpy = jasmine.createSpyObj('UnitOfMeasurementService', ['getUnits']);

    await TestBed.configureTestingModule({
      declarations: [ItemsComponent],
      imports: [FormsModule],
      providers: [
        { provide: ItemService, useValue: itemServiceSpy },
        { provide: CategoryService, useValue: categoryServiceSpy },
        { provide: UnitOfMeasurementService, useValue: unitServiceSpy }
      ]
    }).compileComponents();
  });

  beforeEach(() => {
    itemServiceSpy.getItems.and.returnValue(of(mockItems));
    categoryServiceSpy.getCategories.and.returnValue(of(mockCategories));
    unitServiceSpy.getUnits.and.returnValue(of(mockUnits));
    fixture = TestBed.createComponent(ItemsComponent);
    component = fixture.componentInstance;
  });

  it('should create component', () => {
    expect(component).toBeTruthy();
  });

  it('should load items and categories on init', () => {
    fixture.detectChanges();

    expect(itemServiceSpy.getItems).toHaveBeenCalled();
    expect(categoryServiceSpy.getCategories).toHaveBeenCalled();
    expect(component.items).toHaveSize(1);
    expect(component.categories).toHaveSize(1);
    expect(component.isLoading).toBeFalse();
  });

  it('should handle item loading failure', () => {
    itemServiceSpy.getItems.and.returnValue(throwError(() => new Error('Load failed')));

    fixture.detectChanges();

    expect(component.isLoading).toBeFalse();
    expect(component.loadError).toBe('Failed to load items. Please check the server connection.');
  });

  it('should open and close create item form modal', () => {
    component.openAddForm();
    expect(component.showForm).toBeTrue();
    expect(component.editingId).toBeNull();
    expect(component.form.name).toBe('');

    component.closeForm();
    expect(component.showForm).toBeFalse();
  });

  it('should open edit form with item data', () => {
    component.openEditForm(mockItems[0]);
    expect(component.showForm).toBeTrue();
    expect(component.editingId).toBe(1);
    expect(component.form.name).toBe('Burger');
  });

  it('should save new item', () => {
    itemServiceSpy.createItem.and.returnValue(of(mockItems[0]));
    component.openAddForm();
    component.form.name = 'New Burger';
    component.form.price = 200;
    component.form.unitId = 1;

    component.save();

    expect(itemServiceSpy.createItem).toHaveBeenCalled();
    expect(component.showForm).toBeFalse();
  });

  it('should delete item when confirmed', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    itemServiceSpy.deleteItem.and.returnValue(of(void 0));

    component.deleteItem(mockItems[0]);

    expect(itemServiceSpy.deleteItem).toHaveBeenCalledWith(1);
  });
});
