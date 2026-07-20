import { Component, OnInit } from '@angular/core';
import { ItemService } from '../../../services/item.service';
import { CategoryService } from '../../../services/category.service';
import { Item, Category } from '../../../models/item.model';

interface ItemFormModel {
  name: string;
  price: number;
  taxPercentage: number;
  categoryId: number | null;
  hsnCode: string;
  barcode: string;
  stockQuantity: number;
  trackInventory: boolean;
}

function emptyForm(): ItemFormModel {
  return {
    name: '',
    price: 0,
    taxPercentage: 0,
    categoryId: null,
    hsnCode: '',
    barcode: '',
    stockQuantity: 0,
    trackInventory: false
  };
}

@Component({
  standalone: false,
  selector: 'app-items',
  templateUrl: './items.component.html',
})
export class ItemsComponent implements OnInit {
  items: Item[] = [];
  categories: Category[] = [];
  isLoading = false;
  loadError = '';
  actionError = '';

  showForm = false;
  editingId: number | null = null;
  form: ItemFormModel = emptyForm();
  isSaving = false;

  constructor(
    private readonly itemService: ItemService,
    private readonly categoryService: CategoryService
  ) {}

  ngOnInit(): void {
    this.loadItems();
    this.categoryService.getCategories().subscribe({
      next: (categories) => (this.categories = categories),
      error: (err) => console.error('Categories load error:', err)
    });
  }

  loadItems(): void {
    this.isLoading = true;
    this.loadError = '';
    this.itemService.getItems().subscribe({
      next: (items) => {
        this.items = items;
        this.isLoading = false;
      },
      error: (err) => {
        this.loadError = 'Failed to load items. Please check the server connection.';
        this.isLoading = false;
        console.error('Items load error:', err);
      }
    });
  }

  categoryName(categoryId?: number): string {
    if (!categoryId) return '—';
    return this.categories.find((c) => c.id === categoryId)?.name ?? '—';
  }

  openAddForm(): void {
    this.editingId = null;
    this.form = emptyForm();
    this.actionError = '';
    this.showForm = true;
  }

  openEditForm(item: Item): void {
    this.editingId = item.id;
    this.form = {
      name: item.name,
      price: item.price,
      taxPercentage: item.taxPercentage,
      categoryId: item.categoryId ?? null,
      hsnCode: item.hsnCode ?? '',
      barcode: item.barcode ?? '',
      stockQuantity: item.stockQuantity,
      trackInventory: item.trackInventory
    };
    this.actionError = '';
    this.showForm = true;
  }

  closeForm(): void {
    this.showForm = false;
    this.editingId = null;
  }

  save(): void {
    if (!this.form.name.trim() || this.form.price <= 0 || this.isSaving) return;
    this.isSaving = true;
    this.actionError = '';

    const payload: Partial<Item> = {
      name: this.form.name.trim(),
      price: this.form.price,
      taxPercentage: this.form.taxPercentage,
      categoryId: this.form.categoryId ?? undefined,
      hsnCode: this.form.hsnCode || undefined,
      barcode: this.form.barcode || undefined,
      stockQuantity: this.form.stockQuantity,
      trackInventory: this.form.trackInventory
    };

    const request$ = this.editingId
      ? this.itemService.updateItem(this.editingId, payload)
      : this.itemService.createItem(payload);

    request$.subscribe({
      next: () => {
        this.isSaving = false;
        this.closeForm();
        this.loadItems();
      },
      error: (err) => {
        this.isSaving = false;
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to save item.';
        console.error('Item save error:', err);
      }
    });
  }

  deleteItem(item: Item): void {
    if (!confirm(`Delete item "${item.name}"?`)) return;
    this.itemService.deleteItem(item.id).subscribe({
      next: () => this.loadItems(),
      error: (err) => {
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to delete item.';
        console.error('Item delete error:', err);
      }
    });
  }

  trackByItemId(_index: number, item: Item): number {
    return item.id;
  }
}
