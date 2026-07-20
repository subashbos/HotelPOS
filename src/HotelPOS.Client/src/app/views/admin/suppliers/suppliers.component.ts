import { Component, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { SupplierService } from '../../../services/supplier.service';
import { Supplier } from '../../../models/supplier.model';

type SupplierFormModel = Omit<Supplier, 'id'>;

function emptyForm(): SupplierFormModel {
  return {
    name: '',
    contactPerson: '',
    phone: '',
    email: '',
    address: '',
    gstin: '',
    city: '',
    state: '',
    pincode: '',
    openingBalance: 0,
    creditLimit: 0,
    paymentTerms: ''
  };
}

@Component({
  standalone: false,
  selector: 'app-suppliers',
  templateUrl: './suppliers.component.html',
})
export class SuppliersComponent implements OnInit {
  suppliers: Supplier[] = [];
  isLoading = false;
  loadError = '';
  actionError = '';

  showForm = false;
  editingId: number | null = null;
  form: SupplierFormModel = emptyForm();
  isSaving = false;

  constructor(private readonly supplierService: SupplierService) {}

  ngOnInit(): void {
    this.loadSuppliers();
  }

  loadSuppliers(): void {
    this.isLoading = true;
    this.loadError = '';
    this.supplierService.getSuppliers().subscribe({
      next: (suppliers) => {
        this.suppliers = suppliers;
        this.isLoading = false;
      },
      error: (err) => {
        this.loadError = 'Failed to load suppliers. Please check the server connection.';
        this.isLoading = false;
        console.error('Suppliers load error:', err);
      }
    });
  }

  openAddForm(): void {
    this.editingId = null;
    this.form = emptyForm();
    this.actionError = '';
    this.showForm = true;
  }

  openEditForm(supplier: Supplier): void {
    this.editingId = supplier.id;
    this.form = {
      name: supplier.name,
      contactPerson: supplier.contactPerson ?? '',
      phone: supplier.phone ?? '',
      email: supplier.email ?? '',
      address: supplier.address ?? '',
      gstin: supplier.gstin ?? '',
      city: supplier.city ?? '',
      state: supplier.state ?? '',
      pincode: supplier.pincode ?? '',
      openingBalance: supplier.openingBalance,
      creditLimit: supplier.creditLimit,
      paymentTerms: supplier.paymentTerms ?? ''
    };
    this.actionError = '';
    this.showForm = true;
  }

  closeForm(): void {
    this.showForm = false;
    this.editingId = null;
  }

  save(): void {
    if (!this.form.name.trim() || this.isSaving) return;
    this.isSaving = true;
    this.actionError = '';

    const request$: Observable<unknown> = this.editingId
      ? this.supplierService.updateSupplier(this.editingId, this.form)
      : this.supplierService.createSupplier(this.form);

    request$.subscribe({
      next: () => {
        this.isSaving = false;
        this.closeForm();
        this.loadSuppliers();
      },
      error: (err) => {
        this.isSaving = false;
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to save supplier.';
        console.error('Supplier save error:', err);
      }
    });
  }

  deleteSupplier(supplier: Supplier): void {
    if (!confirm(`Delete supplier "${supplier.name}"?`)) return;
    this.supplierService.deleteSupplier(supplier.id).subscribe({
      next: () => this.loadSuppliers(),
      error: (err) => {
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to delete supplier.';
        console.error('Supplier delete error:', err);
      }
    });
  }

  trackBySupplierId(_index: number, supplier: Supplier): number {
    return supplier.id;
  }
}
