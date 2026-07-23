export interface BomIngredientRow {
  id?: number;
  rawMaterialId: number;
  rawMaterialName: string;
  unit: string;
  quantityRequired: number;
  wastagePercentage: number;
  effectiveQuantity: number;
  costPerUnit: number;
  wastageCost: number;
  ingredientCost: number;
}

export interface MenuItemBom {
  menuItemId: number;
  menuItemName: string;
  menuPrice: number;
  ingredients: BomIngredientRow[];
  totalNetCost: number;
  totalWastageCost: number;
  totalFoodCost: number;
  grossMargin: number;
}
