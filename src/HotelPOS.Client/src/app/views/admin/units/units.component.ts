import { Component, OnInit } from '@angular/core';
import { UnitOfMeasurementService } from '../../../services/unit-of-measurement.service';
import { UnitOfMeasurement } from '../../../models/item.model';

@Component({
  standalone: false,
  selector: 'app-units',
  templateUrl: './units.component.html',
})
export class UnitsComponent implements OnInit {
  units: UnitOfMeasurement[] = [];
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

  constructor(private readonly unitService: UnitOfMeasurementService) {}

  ngOnInit(): void {
    this.loadUnits();
  }

  loadUnits(): void {
    this.isLoading = true;
    this.loadError = '';
    this.unitService.getUnits().subscribe({
      next: (units) => {
        this.units = units.sort((a, b) => a.displayOrder - b.displayOrder);
        this.isLoading = false;
      },
      error: (err) => {
        this.loadError = 'Failed to load units. Please check the server connection.';
        this.isLoading = false;
        console.error('Units load error:', err);
      }
    });
  }

  addUnit(): void {
    if (!this.newName.trim() || this.isSaving) return;
    this.isSaving = true;
    this.saveError = '';
    this.unitService.createUnit({ name: this.newName.trim(), displayOrder: this.newDisplayOrder }).subscribe({
      next: () => {
        this.isSaving = false;
        this.newName = '';
        this.newDisplayOrder = 0;
        this.loadUnits();
      },
      error: (err) => {
        this.isSaving = false;
        this.saveError = err.error?.message || err.error?.Message || err.error || 'Failed to create unit.';
        console.error('Unit create error:', err);
      }
    });
  }

  startEdit(unit: UnitOfMeasurement): void {
    this.editingId = unit.id;
    this.editName = unit.name;
    this.editDisplayOrder = unit.displayOrder;
  }

  cancelEdit(): void {
    this.editingId = null;
  }

  saveEdit(unit: UnitOfMeasurement): void {
    if (!this.editName.trim()) return;
    this.unitService.updateUnit(unit.id, { name: this.editName.trim(), displayOrder: this.editDisplayOrder }).subscribe({
      next: () => {
        this.editingId = null;
        this.loadUnits();
      },
      error: (err) => {
        this.saveError = err.error?.message || err.error?.Message || err.error || 'Failed to update unit.';
        console.error('Unit update error:', err);
      }
    });
  }

  deleteUnit(unit: UnitOfMeasurement): void {
    if (!confirm(`Delete unit "${unit.name}"?`)) return;
    this.unitService.deleteUnit(unit.id).subscribe({
      next: () => this.loadUnits(),
      error: (err) => {
        this.saveError = err.error?.message || err.error?.Message || err.error || 'Failed to delete unit.';
        console.error('Unit delete error:', err);
      }
    });
  }

  trackByUnitId(_index: number, unit: UnitOfMeasurement): number {
    return unit.id;
  }
}
