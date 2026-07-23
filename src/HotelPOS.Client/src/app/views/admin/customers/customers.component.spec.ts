import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { CustomersComponent } from './customers.component';
import { CustomerService } from '../../../services/customer.service';
import { Customer } from '../../../models/customer.model';

describe('CustomersComponent', () => {
  let component: CustomersComponent;
  let fixture: ComponentFixture<CustomersComponent>;
  let customerServiceSpy: jasmine.SpyObj<CustomerService>;

  const mockCustomers: Customer[] = [
    { id: 1, name: 'John Doe', phone: '1234567890', isActive: true, createdAt: '2026-01-01' }
  ];

  beforeEach(async () => {
    customerServiceSpy = jasmine.createSpyObj('CustomerService', [
      'getCustomers',
      'createCustomer',
      'updateCustomer',
      'deleteCustomer'
    ]);

    await TestBed.configureTestingModule({
      declarations: [CustomersComponent],
      imports: [FormsModule],
      providers: [
        { provide: CustomerService, useValue: customerServiceSpy }
      ]
    }).compileComponents();
  });

  beforeEach(() => {
    customerServiceSpy.getCustomers.and.returnValue(of(mockCustomers));
    fixture = TestBed.createComponent(CustomersComponent);
    component = fixture.componentInstance;
  });

  it('should create component', () => {
    expect(component).toBeTruthy();
  });

  it('should load customers on init', () => {
    fixture.detectChanges();

    expect(customerServiceSpy.getCustomers).toHaveBeenCalled();
    expect(component.allCustomers).toHaveSize(1);
    expect(component.isLoading).toBeFalse();
  });

  it('should handle load error', () => {
    spyOn(console, 'error');
    customerServiceSpy.getCustomers.and.returnValue(throwError(() => new Error('Error')));

    fixture.detectChanges();

    expect(component.isLoading).toBeFalse();
    expect(component.loadError).toBe('Failed to load customers. Please check the server connection.');
  });

  it('should open and close form modal', () => {
    component.openAddForm();
    expect(component.showForm).toBeTrue();

    component.cancelForm();
    expect(component.showForm).toBeFalse();
  });

  it('should open edit form with customer data', () => {
    component.openEditForm(mockCustomers[0]);
    expect(component.showForm).toBeTrue();
    expect(component.form.name).toBe('John Doe');
  });

  it('should save new customer', () => {
    customerServiceSpy.createCustomer.and.returnValue(of(mockCustomers[0]));
    component.openAddForm();
    component.form.name = 'Jane Doe';

    component.saveCustomer();

    expect(customerServiceSpy.createCustomer).toHaveBeenCalled();
    expect(component.showForm).toBeFalse();
  });

  it('should delete customer when confirmed', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    customerServiceSpy.deleteCustomer.and.returnValue(of(void 0));

    component.deactivateCustomer(mockCustomers[0]);

    expect(customerServiceSpy.deleteCustomer).toHaveBeenCalledWith(1);
  });

  it('should filter customers by name, phone, email, and gstin including null values', () => {
    component.allCustomers = [
      { id: 1, name: 'Alice Smith', phone: '9876543210', email: 'alice@example.com', gstin: '27AAAAA0000A1Z5', isActive: true, createdAt: '' },
      { id: 2, name: 'Bob Jones', phone: undefined, email: undefined, gstin: undefined, isActive: true, createdAt: '' }
    ];

    // Filter by name
    component.searchQuery = 'alice';
    component.applyFilter();
    expect(component.filteredCustomers.map(c => c.id)).toEqual([1]);

    // Filter by phone
    component.searchQuery = '9876';
    component.applyFilter();
    expect(component.filteredCustomers.map(c => c.id)).toEqual([1]);

    // Filter by email
    component.searchQuery = 'alice@';
    component.applyFilter();
    expect(component.filteredCustomers.map(c => c.id)).toEqual([1]);

    // Filter by gstin
    component.searchQuery = '27AAAAA';
    component.applyFilter();
    expect(component.filteredCustomers.map(c => c.id)).toEqual([1]);

    // Search matching bob with nulls
    component.searchQuery = 'bob';
    component.onSearchChanged();
    expect(component.filteredCustomers.map(c => c.id)).toEqual([2]);

    // Empty search returns all
    component.searchQuery = '   ';
    component.applyFilter();
    expect(component.filteredCustomers).toHaveSize(2);
  });

  it('should validate customer name before saving', () => {
    component.openAddForm();
    component.form.name = '   ';
    component.saveCustomer();
    expect(component.formError).toBe('Customer Name is required.');
    expect(customerServiceSpy.createCustomer).not.toHaveBeenCalled();
  });

  it('should update existing customer when isEditMode is true', () => {
    customerServiceSpy.updateCustomer.and.returnValue(of(void 0));
    component.openEditForm(mockCustomers[0]);
    component.form.name = 'John Updated';
    component.saveCustomer();
    expect(customerServiceSpy.updateCustomer).toHaveBeenCalledWith(1, component.form);
    expect(component.showForm).toBeFalse();
  });

  it('should handle save error with string error vs default fallback', () => {
    customerServiceSpy.createCustomer.and.returnValue(throwError(() => ({ error: 'Duplicate phone' })));
    component.openAddForm();
    component.form.name = 'New Name';
    component.saveCustomer();
    expect(component.formError).toBe('Duplicate phone');

    customerServiceSpy.createCustomer.and.returnValue(throwError(() => ({})));
    component.saveCustomer();
    expect(component.formError).toBe('Failed to save customer.');
  });

  it('should cancel customer deactivation if unconfirmed or handle deactivate error', () => {
    const confirmSpy = spyOn(window, 'confirm');
    confirmSpy.and.returnValue(false);
    component.deactivateCustomer(mockCustomers[0]);
    expect(customerServiceSpy.deleteCustomer).not.toHaveBeenCalled();

    confirmSpy.and.returnValue(true);
    customerServiceSpy.deleteCustomer.and.returnValue(throwError(() => new Error('Error')));
    component.deactivateCustomer(mockCustomers[0]);
    expect(component.loadError).toBe('Failed to deactivate customer.');
  });

  it('should view history and handle success / error / close history', () => {
    customerServiceSpy.getCustomerHistory = jasmine.createSpy('getCustomerHistory').and.returnValue(of({ customer: mockCustomers[0], orders: [] } as any));
    component.viewHistory(mockCustomers[0]);
    expect(component.showHistory).toBeTrue();
    expect(component.history).toBeTruthy();
    expect(component.isHistoryLoading).toBeFalse();

    component.closeHistory();
    expect(component.showHistory).toBeFalse();
    expect(component.history).toBeNull();

    customerServiceSpy.getCustomerHistory.and.returnValue(throwError(() => new Error('Err')));
    component.viewHistory(mockCustomers[0]);
    expect(component.isHistoryLoading).toBeFalse();
  });
});
