import { Component, OnInit } from '@angular/core';
import { CustomerService } from '../../../services/customer.service';
import { Customer, CustomerHistory, SaveCustomerRequest } from '../../../models/customer.model';

@Component({
  standalone: false,
  selector: 'app-customers',
  templateUrl: './customers.component.html',
})
export class CustomersComponent implements OnInit {
  // ── List State ──
  allCustomers: Customer[] = [];
  filteredCustomers: Customer[] = [];
  searchQuery = '';
  isLoading = false;
  loadError = '';

  // ── Form State ──
  showForm = false;
  isEditMode = false;
  form: SaveCustomerRequest = this.emptyForm();
  formError = '';
  isSaving = false;

  // ── History State ──
  showHistory = false;
  history: CustomerHistory | null = null;
  isHistoryLoading = false;

  constructor(private readonly customerService: CustomerService) { }

  ngOnInit(): void {
    this.loadCustomers();
  }

  private emptyForm(): SaveCustomerRequest {
    return { id: 0, name: '', phone: '', email: '', address: '', gstin: '', notes: '' };
  }

  loadCustomers(): void {
    this.isLoading = true;
    this.loadError = '';
    this.customerService.getCustomers().subscribe({
      next: (customers) => {
        this.allCustomers = customers;
        this.applyFilter();
        this.isLoading = false;
      },
      error: () => {
        this.loadError = 'Failed to load customers. Please check the server connection.';
        this.isLoading = false;
      }
    });
  }

  applyFilter(): void {
    const search = this.searchQuery.trim().toLowerCase();
    this.filteredCustomers = !search
      ? this.allCustomers
      : this.allCustomers.filter(c =>
          c.name.toLowerCase().includes(search) ||
          (c.phone ?? '').includes(search) ||
          (c.email ?? '').toLowerCase().includes(search) ||
          (c.gstin ?? '').toLowerCase().includes(search));
  }

  onSearchChanged(): void {
    this.applyFilter();
  }

  openAddForm(): void {
    this.form = this.emptyForm();
    this.isEditMode = false;
    this.formError = '';
    this.showForm = true;
  }

  openEditForm(customer: Customer): void {
    this.form = {
      id: customer.id,
      name: customer.name,
      phone: customer.phone,
      email: customer.email,
      address: customer.address,
      gstin: customer.gstin,
      notes: customer.notes
    };
    this.isEditMode = true;
    this.formError = '';
    this.showForm = true;
  }

  cancelForm(): void {
    this.showForm = false;
    this.formError = '';
  }

  saveCustomer(): void {
    if (!this.form.name?.trim()) {
      this.formError = 'Customer Name is required.';
      return;
    }

    this.isSaving = true;
    this.formError = '';

    const onDone = () => {
      this.isSaving = false;
      this.showForm = false;
      this.loadCustomers();
    };
    const onError = (err: { error?: string }) => {
      this.isSaving = false;
      this.formError = typeof err?.error === 'string' ? err.error : 'Failed to save customer.';
    };

    if (this.isEditMode) {
      this.customerService.updateCustomer(this.form.id, this.form).subscribe({ next: onDone, error: onError });
    } else {
      this.customerService.createCustomer(this.form).subscribe({ next: onDone, error: onError });
    }
  }

  deactivateCustomer(customer: Customer): void {
    if (!confirm(`Deactivate customer '${customer.name}'? Their order history will be preserved.`)) return;

    this.customerService.deleteCustomer(customer.id).subscribe({
      next: () => this.loadCustomers(),
      error: () => { this.loadError = 'Failed to deactivate customer.'; }
    });
  }

  viewHistory(customer: Customer): void {
    this.showHistory = true;
    this.isHistoryLoading = true;
    this.history = null;

    this.customerService.getCustomerHistory(customer.id).subscribe({
      next: (history) => {
        this.history = history;
        this.isHistoryLoading = false;
      },
      error: () => {
        this.isHistoryLoading = false;
      }
    });
  }

  closeHistory(): void {
    this.showHistory = false;
    this.history = null;
  }
}
