import { Component, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { TableService } from '../../../services/table.service';
import { DiningTable } from '../../../models/table.model';

interface TableFormModel {
  number: number;
  name: string;
  capacity: number;
  isActive: boolean;
}

function emptyForm(): TableFormModel {
  return { number: 0, name: '', capacity: 2, isActive: true };
}

@Component({
  standalone: false,
  selector: 'app-tables',
  templateUrl: './tables.component.html',
})
export class TablesComponent implements OnInit {
  tables: DiningTable[] = [];
  isLoading = false;
  loadError = '';
  actionError = '';

  showForm = false;
  editingId: number | null = null;
  form: TableFormModel = emptyForm();
  isSaving = false;

  constructor(private readonly tableService: TableService) {}

  ngOnInit(): void {
    this.loadTables();
  }

  loadTables(): void {
    this.isLoading = true;
    this.loadError = '';
    this.tableService.getTables().subscribe({
      next: (tables) => {
        this.tables = tables.filter((t) => !t.isDeleted).sort((a, b) => a.number - b.number);
        this.isLoading = false;
      },
      error: (err) => {
        this.loadError = 'Failed to load tables. Please check the server connection.';
        this.isLoading = false;
        console.error('Tables load error:', err);
      }
    });
  }

  openAddForm(): void {
    this.editingId = null;
    this.form = emptyForm();
    this.actionError = '';
    this.showForm = true;
  }

  openEditForm(table: DiningTable): void {
    this.editingId = table.id;
    this.form = { number: table.number, name: table.name, capacity: table.capacity, isActive: table.isActive };
    this.actionError = '';
    this.showForm = true;
  }

  closeForm(): void {
    this.showForm = false;
    this.editingId = null;
  }

  save(): void {
    if (!this.form.name.trim() || this.form.number <= 0 || this.isSaving) return;
    this.isSaving = true;
    this.actionError = '';

    const request$: Observable<unknown> = this.editingId
      ? this.tableService.updateTable(this.editingId, this.form)
      : this.tableService.createTable(this.form);

    request$.subscribe({
      next: () => {
        this.isSaving = false;
        this.closeForm();
        this.loadTables();
      },
      error: (err) => {
        this.isSaving = false;
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to save table.';
        console.error('Table save error:', err);
      }
    });
  }

  deleteTable(table: DiningTable): void {
    if (!confirm(`Delete table "${table.name}"?`)) return;
    this.tableService.deleteTable(table.id).subscribe({
      next: () => this.loadTables(),
      error: (err) => {
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to delete table.';
        console.error('Table delete error:', err);
      }
    });
  }

  trackByTableId(_index: number, table: DiningTable): number {
    return table.id;
  }
}
