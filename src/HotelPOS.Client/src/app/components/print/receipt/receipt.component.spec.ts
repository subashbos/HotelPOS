import { TestBed, ComponentFixture } from '@angular/core/testing';
import { ReceiptComponent } from './receipt.component';
import { ReceiptData } from '../../../models/print.model';
import { SystemSettings } from '../../../models/settings.model';

describe('ReceiptComponent', () => {
  let component: ReceiptComponent;
  let fixture: ComponentFixture<ReceiptComponent>;

  const receiptData: ReceiptData = {
    orderId: 1,
    createdAt: new Date(),
    items: [
      { itemName: 'Butter Chicken', price: 340, quantity: 1, total: 357, taxPercentage: 5 },
      { itemName: 'Veg Fried Rice', price: 180, quantity: 2, total: 396, taxPercentage: 10 }
    ],
    subtotal: 700,
    discountAmount: 0,
    totalAmount: 753.4,
    paymentMode: 'Cash'
  };

  const baseSettings = {
    receiptFormat: 'Thermal',
    showGstBreakdown: true,
    enableRoundOff: true,
    isCompositionScheme: false
  } as SystemSettings;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ReceiptComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(ReceiptComponent);
    component = fixture.componentInstance;
    component.data = receiptData;
    component.settings = baseSettings;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should report thermal format from settings', () => {
    component.settings = { ...baseSettings, receiptFormat: 'Thermal' };
    expect(component.isThermal).toBeTrue();

    component.settings = { ...baseSettings, receiptFormat: 'A4' };
    expect(component.isThermal).toBeFalse();
  });

  it('should title the receipt based on composition scheme', () => {
    component.settings = { ...baseSettings, isCompositionScheme: true };
    expect(component.receiptTitle).toBe('BILL OF SUPPLY');

    component.settings = { ...baseSettings, isCompositionScheme: false };
    expect(component.receiptTitle).toBe('TAX INVOICE');
  });

  it('should detect whether customer details are present', () => {
    component.data = { ...receiptData, customerName: undefined, customerPhone: undefined, customerGstin: undefined };
    expect(component.hasCustomer).toBeFalse();

    component.data = { ...receiptData, customerName: 'Walk-in' };
    expect(component.hasCustomer).toBeTrue();
  });

  it('should show the GST breakdown only outside composition scheme', () => {
    component.settings = { ...baseSettings, showGstBreakdown: true, isCompositionScheme: false };
    expect(component.showGstBreakdown).toBeTrue();

    component.settings = { ...baseSettings, showGstBreakdown: true, isCompositionScheme: true };
    expect(component.showGstBreakdown).toBeFalse();
  });

  it('should group items by tax rate and split into CGST/SGST halves', () => {
    const groups = component.taxGroups;

    expect(groups.length).toBe(2);
    // 5% group: 340 * 1 = 340 subtotal, tax = 17, half = 8.5
    expect(groups[0].halfRate).toBe(2.5);
    expect(groups[0].halfTax).toBeCloseTo(8.5);
    // 10% group: 180 * 2 = 360 subtotal, tax = 36, half = 18
    expect(groups[1].halfRate).toBe(5);
    expect(groups[1].halfTax).toBeCloseTo(18);
  });

  it('should skip zero-tax items when grouping', () => {
    component.data = { ...receiptData, items: [{ itemName: 'Water', price: 20, quantity: 1, total: 20, taxPercentage: 0 }] };
    expect(component.taxGroups.length).toBe(0);
  });

  it('should compute round-off difference only when enabled', () => {
    component.settings = { ...baseSettings, enableRoundOff: true };
    component.data = { ...receiptData, totalAmount: 753.4 };
    expect(component.roundOffDiff).toBeCloseTo(-0.4);

    component.settings = { ...baseSettings, enableRoundOff: false };
    expect(component.roundOffDiff).toBe(0);
  });

  it('should compute the grand total with or without rounding', () => {
    component.settings = { ...baseSettings, enableRoundOff: true };
    component.data = { ...receiptData, totalAmount: 753.4 };
    expect(component.grandTotal).toBe(753);

    component.settings = { ...baseSettings, enableRoundOff: false };
    expect(component.grandTotal).toBe(753.4);
  });

  it('should use a shorter, dash separator for thermal and a longer underscore one otherwise', () => {
    component.settings = { ...baseSettings, receiptFormat: 'Thermal' };
    expect(component.separator).toBe('-'.repeat(38));

    component.settings = { ...baseSettings, receiptFormat: 'A4' };
    expect(component.separator).toBe('_'.repeat(86));
  });
});
