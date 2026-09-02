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
      error: (err) => {
        this.errorMessage = 'Failed to load menu items. Please check the server connection.';
        console.error('Menu items load error:', err);
      }
    });
  }

  loadRawMaterials(): void {
    this.rawMaterialService.getRawMaterials().subscribe({
      next: (mats) => {
        this.rawMaterials = mats || [];
      },
      error: (err) => {
        this.errorMessage = 'Failed to load raw materials. Please check the server connection.';
        console.error('Raw materials load error:', err);
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
      error: (err) => {
        this.ingredients = [];
        this.errorMessage = 'Failed to load the recipe for this menu item. Please check the server connection.';
        console.error('BOM load error:', err);
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
      error: (err) => {
        this.isSaving = false;
        this.errorMessage = 'Failed to save the recipe. Please check the server connection and try again.';
        console.error('Recipe save error:', err);
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
