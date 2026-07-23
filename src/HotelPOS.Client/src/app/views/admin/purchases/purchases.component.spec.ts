import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { PurchasesComponent } from './purchases.component';
import { PurchaseService } from '../../../services/purchase.service';
import { ItemService } from '../../../services/item.service';
import { Purchase } from '../../../models/purchase.model';
import { Item } from '../../../models/item.model';

describe('PurchasesComponent', () => {
  let component: PurchasesComponent;
  let fixture: ComponentFixture<PurchasesComponent>;
  let purchaseServiceSpy: jasmine.SpyObj<PurchaseService>;
  let itemServiceSpy: jasmine.SpyObj<ItemService>;

  const mockPurchase: Purchase = {
    sNo: 1,
    id: 1,
    supplierId: 1,
    invoiceNumber: 'INV-001',
    purchaseDate: '2026-01-10',
    paymentType: 'Cash',
    subtotal: 500,
    totalTax: 0,
    totalDiscount: 0,
    grandTotal: 500,
    items: []
  };

  const mockItem: Item = {
    id: 10,
    name: 'Tomato',
    price: 40,
    taxPercentage: 5,
    stockQuantity: 100,
    trackInventory: true
  };

  beforeEach(async () => {
    purchaseServiceSpy = jasmine.createSpyObj('PurchaseService', ['getPurchases', 'getSuppliers', 'createPurchase', 'deletePurchase']);
    itemServiceSpy = jasmine.createSpyObj('ItemService', ['getItems']);

    purchaseServiceSpy.getPurchases.and.returnValue(of([mockPurchase]));
    purchaseServiceSpy.getSuppliers.and.returnValue(of([{ id: 1, name: 'Fresh Veg' } as any]));
    itemServiceSpy.getItems.and.returnValue(of([mockItem]));

    await TestBed.configureTestingModule({
      declarations: [PurchasesComponent],
      imports: [FormsModule],
      providers: [
        { provide: PurchaseService, useValue: purchaseServiceSpy },
        { provide: ItemService, useValue: itemServiceSpy }
      ]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(PurchasesComponent);
    component = fixture.componentInstance;
  });

  it('should create component and load purchases, suppliers, and items', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
    expect(purchaseServiceSpy.getPurchases).toHaveBeenCalled();
    expect(component.purchases.length).toBe(1);
    expect(component.suppliers.length).toBe(1);
    expect(component.catalogItems.length).toBe(1);
  });

  it('should handle purchases load error', () => {
    purchaseServiceSpy.getPurchases.and.returnValue(throwError(() => new Error('Load error')));
    fixture.detectChanges();
    expect(component.isLoading).toBeFalse();
    expect(component.loadError).toBe('Failed to load purchases. Please check the server connection.');
  });

  it('should open form and reset fields', () => {
    component.openForm();
    expect(component.showForm).toBeTrue();
    expect(component.lines.length).toBe(0);

    component.closeForm();
    expect(component.showForm).toBeFalse();
  });

  it('should set unit price and tax on item selected', () => {
    component.catalogItems = [mockItem];
    component.selectedItemId = 10;
    component.onItemSelected();

    expect(component.lineUnitPrice).toBe(40);
    expect(component.lineTaxPercentage).toBe(5);
  });

  it('should add line and calculate totals', () => {
    component.catalogItems = [mockItem];
    component.selectedItemId = 10;
    component.lineQuantity = 2;
    component.lineUnitPrice = 50;
    component.lineTaxPercentage = 10;
    component.lineDiscount = 5;

    component.addLine();

    expect(component.lines.length).toBe(1);
    expect(component.subtotal).toBe(100);
    expect(component.taxTotal).toBe(10);
    expect(component.grandTotal).toBe(105);

    component.removeLine(0);
    expect(component.lines.length).toBe(0);
  });

  it('should save purchase successfully', () => {
    purchaseServiceSpy.createPurchase.and.returnValue(of(mockPurchase));
    component.openForm();
    component.supplierId = 1;
    component.invoiceNumber = 'INV-101';
    component.lines = [{ itemId: 10, itemName: 'Tomato', quantity: 5, unitPrice: 40, taxPercentage: 0, discount: 0, total: 200 }];

    component.savePurchase();

    expect(purchaseServiceSpy.createPurchase).toHaveBeenCalled();
    expect(component.showForm).toBeFalse();
  });

  it('should track purchase by id', () => {
    expect(component.trackByPurchaseId(0, mockPurchase)).toBe(1);
  });
});
