import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { BomComponent } from './bom.component';
import { ItemService } from '../../../services/item.service';
import { RawMaterialService } from '../../../services/raw-material.service';
import { BomService } from '../../../services/bom.service';
import { Item } from '../../../models/item.model';
import { RawMaterial } from '../../../models/raw-material.model';
import { MenuItemBom } from '../../../models/bom.model';

describe('BomComponent', () => {
  let component: BomComponent;
  let fixture: ComponentFixture<BomComponent>;
  let itemServiceSpy: jasmine.SpyObj<ItemService>;
  let rawMaterialServiceSpy: jasmine.SpyObj<RawMaterialService>;
  let bomServiceSpy: jasmine.SpyObj<BomService>;

  const mockItems: Item[] = [
    { id: 1, name: 'Butter Chicken', price: 340, taxPercentage: 5, stockQuantity: 100, trackInventory: true, unitId: 1 } as Item,
    { id: 2, name: 'Veg Fried Rice', price: 180, taxPercentage: 5, stockQuantity: 100, trackInventory: true, unitId: 1 } as Item
  ];
  const mockMaterials: RawMaterial[] = [
    { id: 1, name: 'Chicken', unit: 'kg', costPerUnit: 220, currentStock: 10, minStockThreshold: 2 }
  ];
  const mockBom: MenuItemBom = {
    menuItemId: 1, menuItemName: 'Butter Chicken', menuPrice: 340,
    ingredients: [], totalNetCost: 0, totalWastageCost: 0, totalFoodCost: 0, grossMargin: 340
  };

  beforeEach(async () => {
    itemServiceSpy = jasmine.createSpyObj('ItemService', ['getItems']);
    rawMaterialServiceSpy = jasmine.createSpyObj('RawMaterialService', ['getRawMaterials', 'getMockRawMaterials']);
    bomServiceSpy = jasmine.createSpyObj('BomService', ['getBomForMenuItem', 'saveBom']);

    itemServiceSpy.getItems.and.returnValue(of(mockItems));
    rawMaterialServiceSpy.getRawMaterials.and.returnValue(of(mockMaterials));
    // Return a fresh ingredients array each call so tests that push/splice don't leak into each other.
    bomServiceSpy.getBomForMenuItem.and.callFake(() => of({ ...mockBom, ingredients: [] }));

    await TestBed.configureTestingModule({
      declarations: [BomComponent],
      imports: [FormsModule],
      providers: [
        { provide: ItemService, useValue: itemServiceSpy },
        { provide: RawMaterialService, useValue: rawMaterialServiceSpy },
        { provide: BomService, useValue: bomServiceSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(BomComponent);
    component = fixture.componentInstance;
  });

  it('should create, load menu items, and select the first one', () => {
    fixture.detectChanges();

    expect(component).toBeTruthy();
    expect(component.menuItems.length).toBe(2);
    expect(component.selectedMenuItem?.id).toBe(1);
    expect(bomServiceSpy.getBomForMenuItem).toHaveBeenCalledWith(1);
  });

  it('should surface an error and not fabricate menu items on load error', () => {
    itemServiceSpy.getItems.and.returnValue(throwError(() => new Error('boom')));

    fixture.detectChanges();

    expect(component.menuItems.length).toBe(0);
    expect(component.selectedMenuItem).toBeNull();
    expect(component.errorMessage).toContain('Failed to load menu items');
  });

  it('should filter menu items by search query', () => {
    fixture.detectChanges();

    component.itemSearchQuery = 'fried';
    component.onItemSearchChanged();

    expect(component.filteredMenuItems.length).toBe(1);
    expect(component.filteredMenuItems[0].name).toBe('Veg Fried Rice');
  });

  it('should surface an error and not fabricate raw materials on load error', () => {
    rawMaterialServiceSpy.getRawMaterials.and.returnValue(throwError(() => new Error('boom')));

    fixture.detectChanges();

    expect(component.rawMaterials).toEqual([]);
    expect(component.errorMessage).toContain('Failed to load raw materials');
  });

  it('should surface an error and clear ingredients on BOM load error', () => {
    bomServiceSpy.getBomForMenuItem.and.returnValue(throwError(() => new Error('boom')));

    fixture.detectChanges();

    expect(component.ingredients.length).toBe(0);
    expect(component.errorMessage).toContain('Failed to load the recipe');
  });

  it('should add an ingredient row from the first raw material', () => {
    fixture.detectChanges();
    component.ingredients = [];

    component.addIngredient();

    expect(component.ingredients.length).toBe(1);
    expect(component.ingredients[0].rawMaterialId).toBe(1);
  });

  it('should recalculate a row when the material changes', () => {
    fixture.detectChanges();
    component.addIngredient();
    const row = component.ingredients[0];
    row.quantityRequired = 2;

    component.onMaterialChanged(row, 1);

    expect(row.effectiveQuantity).toBe(2);
    expect(row.ingredientCost).toBe(440);
  });

  it('should compute wastage cost and effective quantity when recalculating a row', () => {
    fixture.detectChanges();
    const row = { rawMaterialId: 1, rawMaterialName: 'Chicken', unit: 'kg', quantityRequired: 1, wastagePercentage: 10, effectiveQuantity: 0, costPerUnit: 100, wastageCost: 0, ingredientCost: 0 };

    component.recalculateRow(row);

    expect(row.effectiveQuantity).toBeCloseTo(1.1);
    expect(row.wastageCost).toBeCloseTo(10);
    expect(row.ingredientCost).toBeCloseTo(110);
  });

  it('should remove an ingredient row', () => {
    fixture.detectChanges();
    component.addIngredient();

    component.removeIngredient(0);

    expect(component.ingredients.length).toBe(0);
  });

  it('should clear the recipe when confirmed', () => {
    fixture.detectChanges();
    spyOn(window, 'confirm').and.returnValue(true);
    component.addIngredient();

    component.clearRecipe();

    expect(component.ingredients.length).toBe(0);
  });

  it('should not clear the recipe when not confirmed', () => {
    fixture.detectChanges();
    spyOn(window, 'confirm').and.returnValue(false);
    component.addIngredient();

    component.clearRecipe();

    expect(component.ingredients.length).toBe(1);
  });

  it('should save the recipe', () => {
    fixture.detectChanges();
    bomServiceSpy.saveBom.and.returnValue(of(mockBom));

    component.saveRecipe();

    expect(bomServiceSpy.saveBom).toHaveBeenCalledWith(1, component.ingredients);
    expect(component.statusMessage).toBe('Recipe saved successfully!');
  });

  it('should surface an error and not claim success when saveBom errors', () => {
    fixture.detectChanges();
    bomServiceSpy.saveBom.and.returnValue(throwError(() => new Error('boom')));

    component.saveRecipe();

    expect(component.statusMessage).toBe('');
    expect(component.errorMessage).toContain('Failed to save the recipe');
  });

  it('should not save when no menu item is selected', () => {
    fixture.detectChanges();
    component.selectedMenuItem = null;

    component.saveRecipe();

    expect(bomServiceSpy.saveBom).not.toHaveBeenCalled();
  });

  it('should compute cost/margin getters from ingredients', () => {
    fixture.detectChanges();
    component.selectedMenuItem = mockItems[0];
    component.ingredients = [
      { rawMaterialId: 1, rawMaterialName: 'Chicken', unit: 'kg', quantityRequired: 1, wastagePercentage: 0, effectiveQuantity: 1, costPerUnit: 100, wastageCost: 0, ingredientCost: 100 }
    ];

    expect(component.totalNetCost).toBe(100);
    expect(component.totalWastageCost).toBe(0);
    expect(component.totalFoodCost).toBe(100);
    expect(component.menuPrice).toBe(340);
    expect(component.grossMarginPercentage).toBeCloseTo(((340 - 100) / 340) * 100);
  });

  it('should return 0 margin when menu price is 0', () => {
    fixture.detectChanges();
    component.selectedMenuItem = null;

    expect(component.grossMarginPercentage).toBe(0);
  });
});
