import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { SuppliersComponent } from './suppliers.component';
import { SupplierService } from '../../../services/supplier.service';
import { Supplier } from '../../../models/supplier.model';

describe('SuppliersComponent', () => {
  let component: SuppliersComponent;
  let fixture: ComponentFixture<SuppliersComponent>;
  let supplierServiceSpy: jasmine.SpyObj<SupplierService>;

  const mockSupplier: Supplier = {
    id: 1,
    name: 'Fresh Veggies Co',
    openingBalance: 0,
    creditLimit: 50000
  };

  beforeEach(async () => {
    supplierServiceSpy = jasmine.createSpyObj('SupplierService', ['getSuppliers', 'createSupplier', 'updateSupplier', 'deleteSupplier']);
    supplierServiceSpy.getSuppliers.and.returnValue(of([mockSupplier]));

    await TestBed.configureTestingModule({
      declarations: [SuppliersComponent],
      imports: [FormsModule],
      providers: [
        { provide: SupplierService, useValue: supplierServiceSpy }
      ]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(SuppliersComponent);
    component = fixture.componentInstance;
  });

  it('should create component and load suppliers', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
    expect(supplierServiceSpy.getSuppliers).toHaveBeenCalled();
    expect(component.suppliers).toHaveSize(1);
  });

  it('should handle suppliers load error', () => {
    supplierServiceSpy.getSuppliers.and.returnValue(throwError(() => new Error('Load error')));
    fixture.detectChanges();
    expect(component.isLoading).toBeFalse();
    expect(component.loadError).toBe('Failed to load suppliers. Please check the server connection.');
  });

  it('should open add form and close form', () => {
    component.openAddForm();
    expect(component.showForm).toBeTrue();
    expect(component.editingId).toBeNull();

    component.closeForm();
    expect(component.showForm).toBeFalse();
  });

  it('should open edit form with supplier data', () => {
    component.openEditForm(mockSupplier);
    expect(component.showForm).toBeTrue();
    expect(component.editingId).toBe(1);
    expect(component.form.name).toBe('Fresh Veggies Co');
  });

  it('should save new supplier', () => {
    supplierServiceSpy.createSupplier.and.returnValue(of(mockSupplier));
    component.openAddForm();
    component.form.name = 'Meat Mart';

    component.save();

    expect(supplierServiceSpy.createSupplier).toHaveBeenCalled();
    expect(component.showForm).toBeFalse();
  });

  it('should update existing supplier', () => {
    supplierServiceSpy.updateSupplier.and.returnValue(of(void 0));
    component.openEditForm(mockSupplier);
    component.form.name = 'Fresh Veggies Ltd';

    component.save();

    expect(supplierServiceSpy.updateSupplier).toHaveBeenCalled();
    expect(component.showForm).toBeFalse();
  });

  it('should delete supplier when confirmed', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    supplierServiceSpy.deleteSupplier.and.returnValue(of(void 0));

    component.deleteSupplier(mockSupplier);

    expect(supplierServiceSpy.deleteSupplier).toHaveBeenCalledWith(1);
  });

  it('should track supplier by id', () => {
    expect(component.trackBySupplierId(0, mockSupplier)).toBe(1);
  });
});
