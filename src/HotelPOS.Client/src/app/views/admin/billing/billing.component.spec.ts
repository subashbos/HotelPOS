import { ElementRef } from '@angular/core';
import { of, throwError } from 'rxjs';
import { BillingComponent } from './billing.component';
import { ItemService } from '../../../services/item.service';
import { OrderService } from '../../../services/order.service';
import { TableService } from '../../../services/table.service';
import { Item } from '../../../models/item.model';
import { ORDER_TYPE_LABELS, PAYMENT_MODES } from '../../../models/order.model';

describe('BillingComponent', () => {
  let component: BillingComponent;
  let itemServiceSpy: jasmine.SpyObj<ItemService>;
  let orderServiceSpy: jasmine.SpyObj<OrderService>;
  let tableServiceSpy: jasmine.SpyObj<TableService>;

  const makeItem = (overrides: Partial<Item> = {}): Item => ({
    id: 1,
    name: 'Chicken Curry',
    price: 200,
    taxPercentage: 5,
    stockQuantity: 10,
    trackInventory: true,
    ...overrides
  });

  beforeEach(() => {
    itemServiceSpy = jasmine.createSpyObj('ItemService', ['getItems']);
    orderServiceSpy = jasmine.createSpyObj('OrderService', ['createOrder']);
    tableServiceSpy = jasmine.createSpyObj('TableService', ['getTables']);
    itemServiceSpy.getItems.and.returnValue(of([]));
    tableServiceSpy.getTables.and.returnValue(of([]));

    component = new BillingComponent(itemServiceSpy, orderServiceSpy, tableServiceSpy);
  });

  describe('cart math', () => {
    it('computeTotal applies tax and rounds to 2 decimals', () => {
      // 2 * 99.995 * 1.05 = 209.9895 -> rounds to 209.99
      expect(component.computeTotal(2, 99.995, 5)).toBeCloseTo(209.99, 2);
    });

    it('addToCart adds a new row with the correct computed total', () => {
      component.addToCart(makeItem({ id: 1, price: 200, taxPercentage: 5 }));

      expect(component.cart).toHaveSize(1);
      expect(component.cart[0].quantity).toBe(1);
      expect(component.cart[0].total).toBe(210); // 1 * 200 * 1.05
    });

    it('addToCart increments quantity instead of duplicating an existing row', () => {
      const item = makeItem({ id: 1, price: 200, taxPercentage: 5 });
      component.addToCart(item);
      component.addToCart(item);

      expect(component.cart).toHaveSize(1);
      expect(component.cart[0].quantity).toBe(2);
      expect(component.cart[0].total).toBe(420); // 2 * 200 * 1.05
    });

    it('addToCart caps quantity to available stock when trackInventory is true', () => {
      const item = makeItem({ id: 1, price: 200, trackInventory: true, stockQuantity: 2 });
      component.addToCart(item);
      component.addToCart(item);
      component.addToCart(item); // 3rd attempt exceeds stock limit of 2

      expect(component.cart[0].quantity).toBe(2);
    });

    it('increaseQty does not increment past available stock when trackInventory is true', () => {
      const item = makeItem({ id: 1, price: 200, trackInventory: true, stockQuantity: 2 });
      component.allItems = [item];
      component.addToCart(item);
      component.increaseQty(component.cart[0]); // qty 2
      component.increaseQty(component.cart[0]); // 3rd attempt

      expect(component.cart[0].quantity).toBe(2);
    });

    it('subtotal, gstAmount, and totalAmount reflect the cart contents', () => {
      component.addToCart(makeItem({ id: 1, price: 200, taxPercentage: 5 }));  // 200, tax 10
      component.addToCart(makeItem({ id: 2, price: 100, taxPercentage: 0 }));  // 100, tax 0

      expect(component.subtotal).toBe(300);
      expect(component.gstAmount).toBe(10);
      expect(component.totalAmount).toBe(310);
    });

    it('totalAmount subtracts the discount and never goes negative', () => {
      component.addToCart(makeItem({ id: 1, price: 100, taxPercentage: 0 }));
      component.discountAmount = 1000; // larger than the order total

      expect(component.totalAmount).toBe(0);
    });

    it('decreaseQty removes the row once quantity would drop to zero', () => {
      component.addToCart(makeItem({ id: 1 }));
      component.decreaseQty(component.cart[0]);

      expect(component.cart).toHaveSize(0);
    });

    it('removeRow renumbers the remaining rows', () => {
      component.addToCart(makeItem({ id: 1 }));
      component.addToCart(makeItem({ id: 2 }));
      component.addToCart(makeItem({ id: 3 }));

      component.removeRow(component.cart[0]); // remove item id 1 (sNo 1)

      expect(component.cart.map(r => r.itemId)).toEqual([2, 3]);
      expect(component.cart.map(r => r.sNo)).toEqual([1, 2]);
    });

    it('decreaseQty decrements quantity without removing the row when above 1', () => {
      component.addToCart(makeItem({ id: 1, price: 100, taxPercentage: 0 }));
      component.increaseQty(component.cart[0]); // qty 2

      component.decreaseQty(component.cart[0]);

      expect(component.cart).toHaveSize(1);
      expect(component.cart[0].quantity).toBe(1);
      expect(component.cart[0].total).toBe(100);
    });

    it('removeRow only clears selectedCartRow when the removed row was the selected one', () => {
      component.addToCart(makeItem({ id: 1 }));
      component.addToCart(makeItem({ id: 2 }));
      component.selectRow(component.cart[1]);

      component.removeRow(component.cart[0]); // remove the row that is NOT selected
      expect(component.selectedCartRow).not.toBeNull();

      component.removeRow(component.selectedCartRow!);
      expect(component.selectedCartRow).toBeNull();
    });
  });

  describe('filteredMenuItems caching', () => {
    it('updateFilteredMenuItems excludes out-of-stock, inventory-tracked items', () => {
      component.allItems = [
        makeItem({ id: 1, name: 'In Stock', trackInventory: true, stockQuantity: 5 }),
        makeItem({ id: 2, name: 'Out Of Stock', trackInventory: true, stockQuantity: 0 }),
        makeItem({ id: 3, name: 'Untracked', trackInventory: false, stockQuantity: 0 })
      ];

      component.updateFilteredMenuItems();

      expect(component.filteredMenuItems.map(i => i.name)).toEqual(['In Stock', 'Untracked']);
    });

    it('selectCategory filters by category and refreshes the cached list', () => {
      component.allItems = [
        makeItem({ id: 1, name: 'Starter', category: { id: 1, name: 'Starters', displayOrder: 1 } }),
        makeItem({ id: 2, name: 'Main', category: { id: 2, name: 'Mains', displayOrder: 2 } })
      ];
      component.updateFilteredMenuItems();

      component.selectCategory('Mains');

      expect(component.selectedCategory).toBe('Mains');
      expect(component.filteredMenuItems.map(i => i.name)).toEqual(['Main']);
    });

    it('updateFilteredMenuItems combines the category filter and the search query', () => {
      component.allItems = [
        makeItem({ id: 1, name: 'Chicken Curry', category: { id: 1, name: 'Mains', displayOrder: 1 } }),
        makeItem({ id: 2, name: 'Chicken Wings', category: { id: 2, name: 'Starters', displayOrder: 2 } }),
        makeItem({ id: 3, name: 'Veg Curry', category: { id: 1, name: 'Mains', displayOrder: 1 } })
      ];
      component.selectedCategory = 'Mains';
      component.searchQuery = 'chicken';

      component.updateFilteredMenuItems();

      expect(component.filteredMenuItems.map(i => i.name)).toEqual(['Chicken Curry']);
    });
  });

  describe('focusSearch', () => {
    it('does nothing when the search element ref is not yet available', () => {
      component.searchEl = undefined as unknown as ElementRef;
      expect(() => component.focusSearch()).not.toThrow();
    });

    it('focuses and selects the search input when the ref is set', () => {
      const focusSpy = jasmine.createSpy('focus');
      const selectSpy = jasmine.createSpy('select');
      component.searchEl = { nativeElement: { focus: focusSpy, select: selectSpy } } as unknown as ElementRef;

      component.focusSearch();

      expect(focusSpy).toHaveBeenCalled();
      expect(selectSpy).toHaveBeenCalled();
    });
  });

  describe('search & autocomplete', () => {
    beforeEach(() => {
      component.allItems = [
        makeItem({ id: 1, name: 'Chicken Curry', trackInventory: true, stockQuantity: 5 }),
        makeItem({ id: 2, name: 'Chicken Wings', trackInventory: true, stockQuantity: 0 }),
        makeItem({ id: 3, name: 'Veg Curry', trackInventory: false, stockQuantity: 0 })
      ];
    });

    it('onSearchQueryChanged clears the autocomplete list and refreshes the menu when the query is blank', () => {
      component.searchQuery = '   ';
      component.autoCompleteItems = [component.allItems[0]];
      component.showAutoComplete = true;

      component.onSearchQueryChanged();

      expect(component.autoCompleteItems).toEqual([]);
      expect(component.showAutoComplete).toBeFalse();
    });

    it('onSearchQueryChanged matches by name case-insensitively and excludes out-of-stock tracked items', () => {
      component.searchQuery = 'CHICKEN';

      component.onSearchQueryChanged();

      expect(component.autoCompleteItems.map(i => i.name)).toEqual(['Chicken Curry']);
      expect(component.showAutoComplete).toBeTrue();
    });

    it('onSearchQueryChanged hides the autocomplete panel when nothing matches', () => {
      component.searchQuery = 'pizza';

      component.onSearchQueryChanged();

      expect(component.autoCompleteItems).toEqual([]);
      expect(component.showAutoComplete).toBeFalse();
    });

    it('addFromAutoComplete adds the item, clears the search state, and refocuses search', () => {
      spyOn(component, 'focusSearch');
      component.searchQuery = 'chicken';
      component.onSearchQueryChanged();

      component.addFromAutoComplete(component.allItems[0]);

      expect(component.cart).toHaveSize(1);
      expect(component.cart[0].itemId).toBe(1);
      expect(component.searchQuery).toBe('');
      expect(component.autoCompleteItems).toEqual([]);
      expect(component.showAutoComplete).toBeFalse();
      expect(component.focusSearch).toHaveBeenCalled();
    });

    it('addFromAutoComplete does nothing when given a falsy item', () => {
      component.addFromAutoComplete(null as unknown as Item);
      expect(component.cart).toHaveSize(0);
    });
  });

  describe('checkout', () => {
    beforeEach(() => {
      component.addToCart(makeItem({ id: 1, name: 'Chicken Curry', price: 200, taxPercentage: 5 }));
      component.tableNumber = '5'; // Dine In (the default order type) requires a table
    });

    it('processCheckout does nothing when the cart is empty', () => {
      component.cart = [];
      component.processCheckout();

      expect(component.showCheckoutModal).toBeFalse();
    });

    it('processCheckout opens the review modal without contacting the backend', () => {
      component.processCheckout();

      expect(component.showCheckoutModal).toBeTrue();
      expect(component.checkoutConfirmed).toBeFalse();
      expect(orderServiceSpy.createOrder).not.toHaveBeenCalled();
    });

    it('confirmCheckout posts the cart to the backend with the mapped order type', () => {
      orderServiceSpy.createOrder.and.returnValue(of(42));
      component.orderType = ORDER_TYPE_LABELS[0];
      component.tableNumber = '7';
      component.processCheckout();

      component.confirmCheckout();

      expect(orderServiceSpy.createOrder).toHaveBeenCalledTimes(1);
      const request = orderServiceSpy.createOrder.calls.mostRecent().args[0];
      expect(request.orderType).toBe('DineIn');
      expect(request.tableNumber).toBe(7);
      expect(request.items).toHaveSize(1);
      expect(request.items[0].itemId).toBe(1);
    });

    it('confirmCheckout marks the order confirmed on success but keeps the cart until the modal is closed', () => {
      orderServiceSpy.createOrder.and.returnValue(of(42));
      component.processCheckout();

      component.confirmCheckout();

      expect(component.checkoutConfirmed).toBeTrue();
      expect(component.lastInvoiceNumber).toBe('Order #42');
      expect(component.cart).toHaveSize(1); // not cleared yet — modal is still open showing the receipt
      expect(component.isCheckingOut).toBeFalse();
    });

    it('closeCheckoutModal clears the cart only after a confirmed checkout', () => {
      orderServiceSpy.createOrder.and.returnValue(of(42));
      component.processCheckout();
      component.confirmCheckout();

      component.closeCheckoutModal();

      expect(component.showCheckoutModal).toBeFalse();
      expect(component.cart).toHaveSize(0);
    });

    it('closeCheckoutModal does not clear the cart if checkout was never confirmed', () => {
      component.processCheckout();

      component.closeCheckoutModal();

      expect(component.cart).toHaveSize(1);
    });

    it('confirmCheckout surfaces the server error message and leaves the cart intact', () => {
      orderServiceSpy.createOrder.and.returnValue(throwError(() => ({ error: { message: 'Insufficient stock for raw material: Chicken.' } })));
      component.processCheckout();

      component.confirmCheckout();

      expect(component.checkoutError).toBe('Insufficient stock for raw material: Chicken.');
      expect(component.checkoutConfirmed).toBeFalse();
      expect(component.isCheckingOut).toBeFalse();
      expect(component.cart).toHaveSize(1);
    });

    it('confirmCheckout maps discount and defaults tableNumber to 0 when non-numeric', () => {
      orderServiceSpy.createOrder.and.returnValue(of(1));
      // Takeaway doesn't require a table, so a non-numeric value can still flow through to 0.
      component.orderType = ORDER_TYPE_LABELS[1];
      component.tableNumber = 'takeaway';
      component.discountAmount = 20;
      component.processCheckout();

      component.confirmCheckout();

      const request = orderServiceSpy.createOrder.calls.mostRecent().args[0];
      expect(request.tableNumber).toBe(0);
      expect(request.discount).toBe(20);
    });

    it('confirmCheckout blocks a Dine In order with no table number and shows an actionable message', () => {
      component.tableNumber = '';
      component.processCheckout();

      component.confirmCheckout();

      expect(component.checkoutError).toBe('Please enter a table number for Dine In orders.');
      expect(orderServiceSpy.createOrder).not.toHaveBeenCalled();
      expect(component.isCheckingOut).toBeFalse();
    });

    it('confirmCheckout does not block Takeaway orders with no table number', () => {
      orderServiceSpy.createOrder.and.returnValue(of(1));
      component.orderType = ORDER_TYPE_LABELS[1];
      component.tableNumber = '';
      component.processCheckout();

      component.confirmCheckout();

      expect(orderServiceSpy.createOrder).toHaveBeenCalledTimes(1);
      expect(component.checkoutError).toBe('');
    });

    it('confirmCheckout omits blank customer fields from the request', () => {
      orderServiceSpy.createOrder.and.returnValue(of(1));
      component.customerName = '';
      component.customerPhone = '';
      component.customerGstin = '';
      component.processCheckout();

      component.confirmCheckout();

      const request = orderServiceSpy.createOrder.calls.mostRecent().args[0];
      expect(request.customerName).toBeUndefined();
      expect(request.customerPhone).toBeUndefined();
      expect(request.customerGstin).toBeUndefined();
    });

    it('confirmCheckout includes customer details when provided', () => {
      orderServiceSpy.createOrder.and.returnValue(of(1));
      component.customerName = 'Alice';
      component.customerPhone = '9999999999';
      component.customerGstin = 'GSTIN123';
      component.processCheckout();

      component.confirmCheckout();

      const request = orderServiceSpy.createOrder.calls.mostRecent().args[0];
      expect(request.customerName).toBe('Alice');
      expect(request.customerPhone).toBe('9999999999');
      expect(request.customerGstin).toBe('GSTIN123');
    });

    it('confirmCheckout falls back to err.error.Message (capital M) when message is absent', () => {
      orderServiceSpy.createOrder.and.returnValue(throwError(() => ({ error: { Message: 'Capitalized message' } })));
      component.processCheckout();

      component.confirmCheckout();

      expect(component.checkoutError).toBe('Capitalized message');
    });

    it('confirmCheckout falls back to a generic message when the server gives no message at all', () => {
      orderServiceSpy.createOrder.and.returnValue(throwError(() => ({ error: null })));
      component.processCheckout();

      component.confirmCheckout();

      expect(component.checkoutError).toBe('Failed to save the order. Please try again.');
    });

    it('confirmCheckout ignores a second click while a request is already in flight', () => {
      orderServiceSpy.createOrder.and.returnValue(of(42));
      component.processCheckout();

      component.isCheckingOut = true;
      component.confirmCheckout();

      expect(orderServiceSpy.createOrder).not.toHaveBeenCalled();
    });

    it('confirmCheckout does nothing once the order has already been confirmed', () => {
      orderServiceSpy.createOrder.and.returnValue(of(42));
      component.processCheckout();
      component.confirmCheckout();

      component.confirmCheckout(); // second call after success

      expect(orderServiceSpy.createOrder).toHaveBeenCalledTimes(1);
    });
  });

  describe('table layout', () => {
    it('openTableLayout(true) loads tables and opens the popup', () => {
      tableServiceSpy.getTables.and.returnValue(of([
        { id: 1, number: 3, name: 'T3', capacity: 4, isActive: true, isDeleted: false },
        { id: 2, number: 1, name: 'T1', capacity: 2, isActive: false, isDeleted: false }
      ]));

      component.openTableLayout(true);

      expect(component.isTableLayoutOpen).toBeTrue();
      expect(component.tables.map(t => t.number)).toEqual([3]); // inactive table filtered out
    });

    it('openTableLayout falls back to a default 20-table layout when none are configured', () => {
      tableServiceSpy.getTables.and.returnValue(of([]));

      component.openTableLayout(true);

      expect(component.tables).toHaveSize(20);
    });

    it('openTableLayout(false) closes the popup without reloading tables', () => {
      component.isTableLayoutOpen = true;

      component.openTableLayout(false);

      expect(component.isTableLayoutOpen).toBeFalse();
      expect(tableServiceSpy.getTables).not.toHaveBeenCalled();
    });

    it('selectTable sets the table number and closes the popup', () => {
      component.isTableLayoutOpen = true;

      component.selectTable(6);

      expect(component.tableNumber).toBe('6');
      expect(component.isTableLayoutOpen).toBeFalse();
    });

    it('isTableOccupied and isTableCurrent reflect held orders and the active selection', () => {
      component.tableNumber = '5';
      component.heldOrders = [
        { holdName: 'H1', heldAt: new Date(), cart: [], discountAmount: 0, paymentMode: 'Cash', orderType: 'DineIn', tableNumber: '2', customerName: '', customerPhone: '', customerGstin: '' }
      ];

      expect(component.isTableOccupied(2)).toBeTrue();
      expect(component.isTableOccupied(9)).toBeFalse();
      expect(component.isTableCurrent(5)).toBeTrue();
      expect(component.isTableCurrent(2)).toBeFalse();
    });

    it('activeTableNumbers combines held-order tables with the current table when the cart has items', () => {
      component.tableNumber = '5';
      component.heldOrders = [
        { holdName: 'H1', heldAt: new Date(), cart: [], discountAmount: 0, paymentMode: 'Cash', orderType: 'DineIn', tableNumber: '2', customerName: '', customerPhone: '', customerGstin: '' }
      ];

      expect(component.activeTableNumbers).toEqual([2]); // empty cart — current table not yet "active"

      component.addToCart(makeItem({ id: 1 }));
      expect(component.activeTableNumbers).toEqual([2, 5]);
    });

    it('trackByTableNumber returns the table number', () => {
      expect(component.trackByTableNumber(0, { id: 1, number: 8, name: 'T8', capacity: 4, isActive: true, isDeleted: false })).toBe(8);
    });
  });

  describe('additional methods', () => {
    it('loadItems successfully sets categories and items', () => {
      const items = [
        makeItem({ id: 1, name: 'Burger', category: { id: 1, name: 'Food', displayOrder: 1 } }),
        makeItem({ id: 2, name: 'Coke', category: { id: 2, name: 'Drinks', displayOrder: 2 } }),
        makeItem({ id: 3, name: 'Untracked', category: undefined })
      ];
      itemServiceSpy.getItems.and.returnValue(of(items));

      component.ngOnInit(); // calls loadItems

      expect(component.allItems).toEqual(items);
      expect(component.categories).toEqual(['All', 'Food', 'Drinks']);
      expect(component.isLoading).toBeFalse();
    });

    it('loadItems handles error', () => {
      const err = new Error('Network error');
      itemServiceSpy.getItems.and.returnValue(throwError(() => err));

      component.loadItems();

      expect(component.loadError).toBe('Failed to load menu items. Please check the server connection.');
      expect(component.isLoading).toBeFalse();
    });

    it('increaseQty increments row quantity and updates total', () => {
      component.addToCart(makeItem({ id: 1, price: 100, taxPercentage: 10 }));
      component.increaseQty(component.cart[0]);

      expect(component.cart[0].quantity).toBe(2);
      expect(component.cart[0].total).toBe(220);
    });

    it('selectRow updates selectedCartRow', () => {
      component.addToCart(makeItem({ id: 1 }));
      const row = component.cart[0];
      component.selectRow(row);

      expect(component.selectedCartRow).toBe(row);
    });

    it('clearCart resets component state', () => {
      component.addToCart(makeItem({ id: 1 }));
      component.discountAmount = 10;
      component.customerName = 'John';
      component.tableNumber = '5';

      component.clearCart();

      expect(component.cart).toHaveSize(0);
      expect(component.discountAmount).toBe(0);
      expect(component.customerName).toBe('');
      expect(component.tableNumber).toBe('');
    });

    it('clearCart also resets orderType and paymentMode to their defaults', () => {
      component.orderType = 'Delivery';
      component.paymentMode = 'Card';
      component.addToCart(makeItem({ id: 1 }));

      component.clearCart();

      expect(component.orderType).toBe(ORDER_TYPE_LABELS[0]);
      expect(component.paymentMode).toBe(PAYMENT_MODES.Cash);
    });

    it('holdOrder and resumeOrder manages held orders list', () => {
      component.addToCart(makeItem({ id: 1, price: 100, taxPercentage: 0 }));
      component.tableNumber = '4';
      component.customerName = 'Alice';

      // hold
      component.holdOrder();
      expect(component.heldOrders).toHaveSize(1);
      expect(component.heldOrders[0].customerName).toBe('Alice');
      expect(component.cart).toHaveSize(0);

      // resume
      component.resumeOrder(component.heldOrders[0]);
      expect(component.cart).toHaveSize(1);
      expect(component.cart[0].itemId).toBe(1);
      expect(component.tableNumber).toBe('4');
      expect(component.heldOrders).toHaveSize(0);
    });

    it('holdOrder does nothing if cart is empty', () => {
      component.holdOrder();
      expect(component.heldOrders).toHaveSize(0);
    });

    it('resumeOrder hides the held-orders panel and refocuses the search input', () => {
      spyOn(component, 'focusSearch');
      component.addToCart(makeItem({ id: 1 }));
      component.holdOrder();
      component.showHeldOrders = true;

      component.resumeOrder(component.heldOrders[0]);

      expect(component.showHeldOrders).toBeFalse();
      expect(component.focusSearch).toHaveBeenCalled();
    });

    it('totalItemsCount sums quantities in cart', () => {
      component.addToCart(makeItem({ id: 1 }));
      component.increaseQty(component.cart[0]);
      component.addToCart(makeItem({ id: 2 }));

      expect(component.totalItemsCount).toBe(3); // 2 + 1
    });

    it('trackBy helpers return correct keys', () => {
      expect(component.trackByItemId(0, makeItem({ id: 9 }))).toBe(9);
      expect(component.trackByCartItemId(0, { sNo: 1, itemId: 12, itemName: 'i', quantity: 1, taxPercentage: 0, price: 10, total: 10 })).toBe(12);
      expect(component.trackByHoldName(0, { holdName: 'H1', heldAt: new Date(), cart: [], discountAmount: 0, paymentMode: 'Cash', orderType: 'Takeaway', tableNumber: '', customerName: '', customerPhone: '', customerGstin: '' })).toBe('H1');
    });

    it('handleKeyboardShortcuts triggers focusSearch on F1/F3 and processCheckout on F4', () => {
      spyOn(component, 'focusSearch');
      spyOn(component, 'processCheckout');

      const f1Event = new KeyboardEvent('keydown', { key: 'F1' });
      const f3Event = new KeyboardEvent('keydown', { key: 'F3' });
      const f4Event = new KeyboardEvent('keydown', { key: 'F4' });
      const otherEvent = new KeyboardEvent('keydown', { key: 'Enter' });

      component.handleKeyboardShortcuts(f1Event);
      expect(component.focusSearch).toHaveBeenCalledTimes(1);

      component.handleKeyboardShortcuts(f3Event);
      expect(component.focusSearch).toHaveBeenCalledTimes(2);

      component.handleKeyboardShortcuts(f4Event);
      expect(component.processCheckout).toHaveBeenCalledTimes(1);

      component.handleKeyboardShortcuts(otherEvent);
      expect(component.focusSearch).toHaveBeenCalledTimes(2);
      expect(component.processCheckout).toHaveBeenCalledTimes(1);
    });
  });
});
