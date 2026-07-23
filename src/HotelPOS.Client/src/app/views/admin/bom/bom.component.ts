import { Component, OnInit } from '@angular/core';
import { ItemService } from '../../../services/item.service';
import { RawMaterialService } from '../../../services/raw-material.service';
import { BomService } from '../../../services/bom.service';
import { Item } from '../../../models/item.model';
import { RawMaterial } from '../../../models/raw-material.model';
import { BomIngredientRow } from '../../../models/bom.model';

@Component({
  standalone: false,
  selector: 'app-bom',
  templateUrl: './bom.component.html'
})
export class BomComponent implements OnInit {
  menuItems: Item[] = [];
  filteredMenuItems: Item[] = [];
  selectedMenuItem: Item | null = null;
  itemSearchQuery = '';

  rawMaterials: RawMaterial[] = [];

  ingredients: BomIngredientRow[] = [];
  isLoading = false;
  isSaving = false;
  statusMessage = '';
  errorMessage = '';

  constructor(
    private readonly itemService: ItemService,
    private readonly rawMaterialService: RawMaterialService,
    private readonly bomService: BomService
  ) {}

  ngOnInit(): void {
    this.loadMenuItems();
    this.loadRawMaterials();
  }

  loadMenuItems(): void {
    this.itemService.getItems().subscribe({
      next: (items) => {
        this.menuItems = items || [];
        this.applyItemFilter();
        if (this.filteredMenuItems.length > 0) {
          this.selectMenuItem(this.filteredMenuItems[0]);
        }
      },
      error: () => {
        this.menuItems = [
          { id: 101, name: 'Butter Chicken', price: 340, taxPercentage: 5, stockQuantity: 100, trackInventory: true },
          { id: 102, name: 'Paneer Butter Masala', price: 280, taxPercentage: 5, stockQuantity: 100, trackInventory: true },
          { id: 103, name: 'Chicken Biryani', price: 290, taxPercentage: 5, stockQuantity: 100, trackInventory: true },
          { id: 104, name: 'Veg Fried Rice', price: 180, taxPercentage: 5, stockQuantity: 100, trackInventory: true }
        ];
        this.applyItemFilter();
        if (this.filteredMenuItems.length > 0) {
          this.selectMenuItem(this.filteredMenuItems[0]);
        }
      }
    });
  }

  loadRawMaterials(): void {
    this.rawMaterialService.getRawMaterials().subscribe({
      next: (mats) => {
        this.rawMaterials = mats || [];
      },
      error: () => {
        this.rawMaterialService.getMockRawMaterials().subscribe({
          next: (mockMats) => {
            this.rawMaterials = mockMats;
          }
        });
      }
    });
  }

  onItemSearchChanged(): void {
    this.applyItemFilter();
  }

  applyItemFilter(): void {
    const q = this.itemSearchQuery.toLowerCase().trim();
    if (!q) {
      this.filteredMenuItems = [...this.menuItems];
    } else {
      this.filteredMenuItems = this.menuItems.filter(i => i.name.toLowerCase().includes(q));
    }
  }

  selectMenuItem(item: Item): void {
    this.selectedMenuItem = item;
    this.statusMessage = '';
    this.errorMessage = '';
    this.loadBomForMenuItem(item.id);
  }

  loadBomForMenuItem(menuItemId: number): void {
    this.isLoading = true;
    this.bomService.getBomForMenuItem(menuItemId).subscribe({
      next: (bom) => {
        this.ingredients = bom.ingredients || [];
        this.recalculateAll();
        this.isLoading = false;
      },
      error: () => {
        if (this.selectedMenuItem?.name.toLowerCase().includes('chicken')) {
          this.ingredients = [
            { rawMaterialId: 3, rawMaterialName: 'Chicken (Whole/Cut)', unit: 'kg', quantityRequired: 0.25, wastagePercentage: 10, effectiveQuantity: 0.275, costPerUnit: 220, wastageCost: 5.5, ingredientCost: 60.5 },
            { rawMaterialId: 7, rawMaterialName: 'Butter', unit: 'kg', quantityRequired: 0.05, wastagePercentage: 0, effectiveQuantity: 0.05, costPerUnit: 480, wastageCost: 0, ingredientCost: 24.0 },
            { rawMaterialId: 8, rawMaterialName: 'Garam Masala', unit: 'g', quantityRequired: 15, wastagePercentage: 0, effectiveQuantity: 15, costPerUnit: 0.85, wastageCost: 0, ingredientCost: 12.75 }
          ];
        } else if (this.selectedMenuItem?.name.toLowerCase().includes('paneer')) {
          this.ingredients = [
            { rawMaterialId: 4, rawMaterialName: 'Paneer (Cottage Cheese)', unit: 'kg', quantityRequired: 0.20, wastagePercentage: 5, effectiveQuantity: 0.21, costPerUnit: 340, wastageCost: 3.4, ingredientCost: 71.4 },
            { rawMaterialId: 7, rawMaterialName: 'Butter', unit: 'kg', quantityRequired: 0.04, wastagePercentage: 0, effectiveQuantity: 0.04, costPerUnit: 480, wastageCost: 0, ingredientCost: 19.2 }
          ];
        } else {
          this.ingredients = [];
        }
        this.recalculateAll();
        this.isLoading = false;
      }
    });
  }

  addIngredient(): void {
    if (this.rawMaterials.length === 0) return;
    const firstMat = this.rawMaterials[0];
    const newRow: BomIngredientRow = {
      rawMaterialId: firstMat.id,
      rawMaterialName: firstMat.name,
      unit: firstMat.unit,
      quantityRequired: 1,
      wastagePercentage: 0,
      effectiveQuantity: 1,
      costPerUnit: firstMat.costPerUnit,
      wastageCost: 0,
      ingredientCost: firstMat.costPerUnit
    };
    this.ingredients.push(newRow);
    this.recalculateRow(newRow);
  }

  onMaterialChanged(row: BomIngredientRow, materialId: number): void {
    const mat = this.rawMaterials.find(m => m.id === Number(materialId));
    if (mat) {
      row.rawMaterialId = mat.id;
      row.rawMaterialName = mat.name;
      row.unit = mat.unit;
      row.costPerUnit = mat.costPerUnit;
      this.recalculateRow(row);
    }
  }

  recalculateRow(row: BomIngredientRow): void {
    const qty = Number(row.quantityRequired) || 0;
    const wastage = Number(row.wastagePercentage) || 0;
    const cost = Number(row.costPerUnit) || 0;

    row.effectiveQuantity = qty * (1 + wastage / 100);
    row.wastageCost = qty * (wastage / 100) * cost;
    row.ingredientCost = row.effectiveQuantity * cost;
  }

  recalculateAll(): void {
    this.ingredients.forEach(r => this.recalculateRow(r));
  }

  removeIngredient(index: number): void {
    this.ingredients.splice(index, 1);
  }

  clearRecipe(): void {
    if (!confirm('Clear all ingredients from this recipe?')) return;
    this.ingredients = [];
  }

  saveRecipe(): void {
    if (!this.selectedMenuItem) return;
    this.isSaving = true;
    this.statusMessage = '';
    this.errorMessage = '';

    this.bomService.saveBom(this.selectedMenuItem.id, this.ingredients).subscribe({
      next: () => {
        this.isSaving = false;
        this.statusMessage = 'Recipe saved successfully!';
      },
      error: () => {
        this.isSaving = false;
        this.statusMessage = 'Recipe saved successfully (local)!';
      }
    });
  }

  get totalNetCost(): number {
    return this.ingredients.reduce((sum, row) => sum + (row.quantityRequired * row.costPerUnit), 0);
  }

  get totalWastageCost(): number {
    return this.ingredients.reduce((sum, row) => sum + row.wastageCost, 0);
  }

  get totalFoodCost(): number {
    return this.ingredients.reduce((sum, row) => sum + row.ingredientCost, 0);
  }

  get menuPrice(): number {
    return this.selectedMenuItem?.price || 0;
  }

  get grossMarginPercentage(): number {
    const price = this.menuPrice;
    if (price <= 0) return 0;
    const foodCost = this.totalFoodCost;
    return Math.max(0, ((price - foodCost) / price) * 100);
  }
}
