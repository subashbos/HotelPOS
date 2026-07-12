import { of, throwError } from 'rxjs';
import { BillingComponent } from './billing.component';
import { ItemService } from '../../../services/item.service';
import { OrderService } from '../../../services/order.service';
import { Item } from '../../../models/item.model';
import { ORDER_TYPE_LABELS, PAYMENT_MODES } from '../../../models/order.model';

describe('BillingComponent', () => {
  let component: BillingComponent;
  let itemServiceSpy: jasmine.SpyObj<ItemService>;
  let orderServiceSpy: jasmine.SpyObj<OrderService>;

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
    itemServiceSpy.getItems.and.returnValue(of([]));

    component = new BillingComponent(itemServiceSpy, orderServiceSpy);
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
  });

  describe('checkout', () => {
    beforeEach(() => {
      component.addToCart(makeItem({ id: 1, name: 'Chicken Curry', price: 200, taxPercentage: 5 }));
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
});
