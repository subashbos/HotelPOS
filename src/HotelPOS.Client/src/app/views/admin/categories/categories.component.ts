import { Component, OnInit } from '@angular/core';
import { CategoryService } from '../../../services/category.service';
import { Category } from '../../../models/item.model';

@Component({
  standalone: false,
  selector: 'app-categories',
  templateUrl: './categories.component.html',
})
export class CategoriesComponent implements OnInit {
  categories: Category[] = [];
  isLoading = false;
  loadError = '';

  // ── Add form ──
  newName = '';
  newDisplayOrder = 0;
  isSaving = false;
  saveError = '';

  // ── Edit state ──
  editingId: number | null = null;
  editName = '';
  editDisplayOrder = 0;

  constructor(private readonly categoryService: CategoryService) {}

  ngOnInit(): void {
    this.loadCategories();
  }

  loadCategories(): void {
    this.isLoading = true;
    this.loadError = '';
    this.categoryService.getCategories().subscribe({
      next: (categories) => {
        this.categories = categories.sort((a, b) => a.displayOrder - b.displayOrder);
        this.isLoading = false;
      },
      error: (err) => {
        this.loadError = 'Failed to load categories. Please check the server connection.';
        this.isLoading = false;
        console.error('Categories load error:', err);
      }
    });
  }

  addCategory(): void {
    if (!this.newName.trim() || this.isSaving) return;
    this.isSaving = true;
    this.saveError = '';
    this.categoryService.createCategory({ name: this.newName.trim(), displayOrder: this.newDisplayOrder }).subscribe({
      next: () => {
        this.isSaving = false;
        this.newName = '';
        this.newDisplayOrder = 0;
        this.loadCategories();
      },
      error: (err) => {
        this.isSaving = false;
        this.saveError = err.error?.message || err.error?.Message || err.error || 'Failed to create category.';
        console.error('Category create error:', err);
      }
    });
  }

  startEdit(category: Category): void {
    this.editingId = category.id;
    this.editName = category.name;
    this.editDisplayOrder = category.displayOrder;
  }

  cancelEdit(): void {
    this.editingId = null;
  }

  saveEdit(category: Category): void {
    if (!this.editName.trim()) return;
    this.categoryService.updateCategory(category.id, { name: this.editName.trim(), displayOrder: this.editDisplayOrder }).subscribe({
      next: () => {
        this.editingId = null;
        this.loadCategories();
      },
      error: (err) => {
        this.saveError = err.error?.message || err.error?.Message || err.error || 'Failed to update category.';
        console.error('Category update error:', err);
      }
    });
  }

  deleteCategory(category: Category): void {
    if (!confirm(`Delete category "${category.name}"?`)) return;
    this.categoryService.deleteCategory(category.id).subscribe({
      next: () => this.loadCategories(),
      error: (err) => {
        this.saveError = err.error?.message || err.error?.Message || err.error || 'Failed to delete category.';
        console.error('Category delete error:', err);
      }
    });
  }

  trackByCategoryId(_index: number, category: Category): number {
    return category.id;
  }
}
