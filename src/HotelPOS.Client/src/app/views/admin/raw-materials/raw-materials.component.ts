import { Component, OnInit } from '@angular/core';
import { RawMaterialService } from '../../../services/raw-material.service';
import { RawMaterial } from '../../../models/raw-material.model';

@Component({
  standalone: false,
  selector: 'app-raw-materials',
  templateUrl: './raw-materials.component.html'
})
export class RawMaterialsComponent implements OnInit {
  rawMaterials: RawMaterial[] = [];
  filteredMaterials: RawMaterial[] = [];
  isLoading = false;
  loadError = '';
  searchQuery = '';

  // Form modal state
  showForm = false;
  isEditMode = false;
  editingId: number | null = null;
  isSaving = false;
  formError = '';

  form: Partial<RawMaterial> = {
    name: '',
    unit: 'kg',
    costPerUnit: 0,
    currentStock: 0,
    minStockThreshold: 0
  };

  unitOptions = ['kg', 'g', 'l', 'ml', 'pcs', 'pack', 'box', 'dozen'];

  constructor(private readonly rawMaterialService: RawMaterialService) {}

  ngOnInit(): void {
    this.loadRawMaterials();
  }

  loadRawMaterials(): void {
    this.isLoading = true;
    this.loadError = '';
    this.rawMaterialService.getRawMaterials().subscribe({
      next: (data) => {
        this.rawMaterials = data || [];
        this.applyFilter();
        this.isLoading = false;
      },
      error: () => {
        this.rawMaterialService.getMockRawMaterials().subscribe({
          next: (mockData) => {
            this.rawMaterials = mockData;
            this.applyFilter();
            this.isLoading = false;
          }
        });
      }
    });
  }

  onSearchChanged(): void {
    this.applyFilter();
  }

  applyFilter(): void {
    const query = this.searchQuery.toLowerCase().trim();
    if (!query) {
      this.filteredMaterials = [...this.rawMaterials];
    } else {
      this.filteredMaterials = this.rawMaterials.filter(m =>
        m.name.toLowerCase().includes(query) ||
        m.unit.toLowerCase().includes(query)
      );
    }
  }

  openAddForm(): void {
    this.isEditMode = false;
    this.editingId = null;
    this.formError = '';
    this.form = {
      name: '',
      unit: 'kg',
      costPerUnit: 0,
      currentStock: 0,
      minStockThreshold: 0
    };
    this.showForm = true;
  }

  openEditForm(material: RawMaterial): void {
    this.isEditMode = true;
    this.editingId = material.id;
    this.formError = '';
    this.form = { ...material };
    this.showForm = true;
  }

  cancelForm(): void {
    this.showForm = false;
  }

  saveRawMaterial(): void {
    if (!this.form.name || !this.form.name.trim()) {
      this.formError = 'Material name is required.';
      return;
    }

    this.isSaving = true;
    this.formError = '';

    if (this.isEditMode && this.editingId) {
      this.rawMaterialService.updateRawMaterial(this.editingId, this.form).subscribe({
        next: () => {
          this.isSaving = false;
          this.showForm = false;
          this.loadRawMaterials();
        },
        error: () => {
          const idx = this.rawMaterials.findIndex(m => m.id === this.editingId);
          if (idx !== -1) {
            this.rawMaterials[idx] = { ...this.rawMaterials[idx], ...this.form as RawMaterial };
            this.applyFilter();
          }
          this.isSaving = false;
          this.showForm = false;
        }
      });
    } else {
      this.rawMaterialService.createRawMaterial(this.form).subscribe({
        next: () => {
          this.isSaving = false;
          this.showForm = false;
          this.loadRawMaterials();
        },
        error: () => {
          const newMat: RawMaterial = {
            id: Date.now(),
            name: this.form.name!.trim(),
            unit: this.form.unit || 'kg',
            costPerUnit: Number(this.form.costPerUnit) || 0,
            currentStock: Number(this.form.currentStock) || 0,
            minStockThreshold: Number(this.form.minStockThreshold) || 0
          };
          this.rawMaterials.unshift(newMat);
          this.applyFilter();
          this.isSaving = false;
          this.showForm = false;
        }
      });
    }
  }

  deleteMaterial(material: RawMaterial): void {
    if (!confirm(`Delete raw material "${material.name}"?`)) return;
    this.rawMaterialService.deleteRawMaterial(material.id).subscribe({
      next: () => this.loadRawMaterials(),
      error: () => {
        this.rawMaterials = this.rawMaterials.filter(m => m.id !== material.id);
        this.applyFilter();
      }
    });
  }
}
