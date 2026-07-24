export interface Category {
  id: number;
  name: string;
  displayOrder: number;
}

export interface UnitOfMeasurement {
  id: number;
  name: string;
  displayOrder: number;
}

export interface Item {
  id: number;
  name: string;
  price: number;
  taxPercentage: number;
  categoryId?: number;
  category?: Category;
  stockQuantity: number;
  trackInventory: boolean;
  hsnCode?: string;
  barcode?: string;
  unitId: number;
}
