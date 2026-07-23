export interface RawMaterial {
  id: number;
  name: string;
  unit: string;
  costPerUnit: number;
  currentStock: number;
  minStockThreshold: number;
}
