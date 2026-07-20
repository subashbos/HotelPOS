import { Component, OnInit, HostListener, ViewChild, ElementRef } from '@angular/core';
import { ItemService } from '../../../services/item.service';
import { OrderService } from '../../../services/order.service';
import { Item } from '../../../models/item.model';
import { CreateOrderRequest, ORDER_TYPE_LABELS, PAYMENT_MODES } from '../../../models/order.model';

interface CartRow {
  sNo: number;
  itemId: number;
  itemName: string;
  quantity: number;
  taxPercentage: number;
  price: number;
  total: number;
}

interface HeldOrder {
  holdName: string;
  heldAt: Date;
  cart: CartRow[];
  customerName: string;
  customerPhone: string;
  customerGstin: string;
  tableNumber: string;
  orderType: string;
  paymentMode: string;
  discountAmount: number;
}

@Component({
  standalone: false,
  
  selector: 'app-billing',
  templateUrl: './billing.component.html',
})
export class BillingComponent implements OnInit {
  @ViewChild('searchEl') searchEl!: ElementRef;

  // ── Menu Data from API ──
  allItems: Item[] = [];
  categories: string[] = ['All'];
  selectedCategory = 'All';

  // Cached rather than recomputed via a getter, since filtering the whole
  // catalog on every change-detection cycle gets expensive as the menu grows.
  filteredMenuItems: Item[] = [];

  // ── State ──
  isLoading = false;
  loadError = '';

  // ── Search & AutoComplete ──
  searchQuery = '';
  showAutoComplete = false;
  autoCompleteItems: Item[] = [];

  // ── Cart ──
  cart: CartRow[] = [];
  selectedCartRow: CartRow | null = null;

  // ── Order Meta ──
  readonly orderTypeLabels = ORDER_TYPE_LABELS;
  orderType: string = ORDER_TYPE_LABELS[0];
  paymentMode: string = PAYMENT_MODES.Cash;

  // ── Customer Details ──
  showCustomerDetails = false;
  customerName = '';
  customerPhone = '';
  customerGstin = '';
  tableNumber = '';

  // ── Discount ──
  discountAmount = 0;

  // ── Checkout ──
  showCheckoutModal = false;
  lastInvoiceNumber = '';
  isCheckingOut = false;
  checkoutConfirmed = false;
  checkoutError = '';

  // ── Hold Orders ──
  heldOrders: HeldOrder[] = [];
  showHeldOrders = false;

  constructor(private readonly itemService: ItemService, private readonly orderService: OrderService) {}

  ngOnInit(): void {
    this.loadItems();
  }

  // ── Load Items from API ──
  loadItems(): void {
    this.isLoading = true;
    this.loadError = '';
    this.itemService.getItems().subscribe({
      next: (items) => {
        this.allItems = items;
        // Build category list dynamically from API data
        const cats = [...new Set(
          items
            .filter(i => i.category?.name)
            .map(i => i.category!.name)
        )];
        this.categories = ['All', ...cats];
        this.isLoading = false;
        this.updateFilteredMenuItems();
      },
      error: (err) => {
        this.loadError = 'Failed to load menu items. Please check the server connection.';
        this.isLoading = false;
        console.error('Items load error:', err);
      }
    });
  }

  // ── Global Keyboard Shortcuts ──
  @HostListener('window:keydown', ['$event'])
  handleKeyboardShortcuts(event: KeyboardEvent): void {
    if (event.key === 'F1' || event.key === 'F3') {
      event.preventDefault();
      this.focusSearch();
    }
    if (event.key === 'F4') {
      event.preventDefault();
      this.processCheckout();
    }
  }

  focusSearch(): void {
    if (this.searchEl) {
      const el = this.searchEl.nativeElement;
      el.focus();
      el.select();
    }
  }

  // ── trackBy functions (avoid re-rendering the whole list on every change-detection cycle) ──
  trackByItemId(_index: number, item: Item): number {
    return item.id;
  }

  trackByCartItemId(_index: number, row: CartRow): number {
    return row.itemId;
  }

  trackByHoldName(_index: number, held: HeldOrder): string {
    return held.holdName;
  }

  // ── Filtered Items (Category + Search) ──
  selectCategory(cat: string): void {
    this.selectedCategory = cat;
    this.updateFilteredMenuItems();
  }

  updateFilteredMenuItems(): void {
    this.filteredMenuItems = this.allItems.filter(item => {
      const matchCat = this.selectedCategory === 'All' ||
        item.category?.name === this.selectedCategory;
      const q = this.searchQuery.toLowerCase();
      const matchSearch = !q || item.name.toLowerCase().includes(q);
      // Skip out-of-stock if trackInventory is on
      const inStock = !item.trackInventory || item.stockQuantity > 0;
      return matchCat && matchSearch && inStock;
    });
  }

  // ── AutoComplete ──
  onSearchQueryChanged(): void {
    const q = this.searchQuery.trim().toLowerCase();
    if (!q) {
      this.autoCompleteItems = [];
      this.showAutoComplete = false;
      this.updateFilteredMenuItems();
      return;
    }
    this.autoCompleteItems = this.allItems.filter(i =>
      i.name.toLowerCase().includes(q) &&
      (!i.trackInventory || i.stockQuantity > 0)
    );
    this.showAutoComplete = this.autoCompleteItems.length > 0;
    this.updateFilteredMenuItems();
  }

  addFromAutoComplete(item: Item): void {
    if (!item) return;
    this.addToCart(item);
    this.searchQuery = '';
    this.autoCompleteItems = [];
    this.showAutoComplete = false;
    this.updateFilteredMenuItems();
    this.focusSearch();
  }

  // ── Cart Operations ──
  computeTotal(qty: number, price: number, tax: number): number {
    return Math.round(qty * price * (1 + tax / 100) * 100) / 100;
  }

  addToCart(item: Item): void {
    const existing = this.cart.find(c => c.itemId === item.id);
    if (existing) {
      existing.quantity += 1;
      existing.total = this.computeTotal(existing.quantity, existing.price, existing.taxPercentage);
    } else {
      this.cart.push({
        sNo: this.cart.length + 1,
        itemId: item.id,
        itemName: item.name,
        quantity: 1,
        taxPercentage: Number(item.taxPercentage),
        price: Number(item.price),
        total: this.computeTotal(1, Number(item.price), Number(item.taxPercentage))
      });
    }
    this.reNumberRows();
  }

  updateRowTotal(row: CartRow): void {
    row.total = this.computeTotal(row.quantity, row.price, row.taxPercentage);
  }

  increaseQty(row: CartRow): void {
    row.quantity += 1;
    this.updateRowTotal(row);
  }

  decreaseQty(row: CartRow): void {
    if (row.quantity > 1) {
      row.quantity -= 1;
      this.updateRowTotal(row);
    } else {
      this.removeRow(row);
    }
  }

  removeRow(row: CartRow): void {
    this.cart = this.cart.filter(c => c.itemId !== row.itemId);
    if (this.selectedCartRow?.itemId === row.itemId) this.selectedCartRow = null;
    this.reNumberRows();
  }

  reNumberRows(): void {
    this.cart.forEach((row, idx) => row.sNo = idx + 1);
  }

  selectRow(row: CartRow): void {
    this.selectedCartRow = row;
  }

  clearCart(): void {
    this.cart = [];
    this.selectedCartRow = null;
    this.discountAmount = 0;
    this.customerName = '';
    this.customerPhone = '';
    this.customerGstin = '';
    this.tableNumber = '';
    this.orderType = ORDER_TYPE_LABELS[0];
    this.paymentMode = PAYMENT_MODES.Cash;
  }

  // ── Hold Orders ──
  holdOrder(): void {
    if (this.cart.length === 0) return;
    const holdName = `Table ${this.tableNumber || (this.heldOrders.length + 1)} – ${new Date().toLocaleTimeString()}`;
    this.heldOrders.push({
      holdName,
      heldAt: new Date(),
      cart: this.cart.map(r => ({ ...r })),
      customerName: this.customerName,
      customerPhone: this.customerPhone,
      customerGstin: this.customerGstin,
      tableNumber: this.tableNumber,
      orderType: this.orderType,
      paymentMode: this.paymentMode,
      discountAmount: this.discountAmount
    });
    this.clearCart();
  }

  resumeOrder(held: HeldOrder): void {
    this.cart = held.cart.map(r => ({ ...r }));
    this.customerName = held.customerName;
    this.customerPhone = held.customerPhone;
    this.customerGstin = held.customerGstin;
    this.tableNumber = held.tableNumber;
    this.orderType = held.orderType;
    this.paymentMode = held.paymentMode;
    this.discountAmount = held.discountAmount;
    this.heldOrders = this.heldOrders.filter(h => h !== held);
    this.showHeldOrders = false;
    this.focusSearch();
  }

  // ── Totals ──
  get subtotal(): number {
    return this.cart.reduce((s, r) => s + r.quantity * r.price, 0);
  }

  get gstAmount(): number {
    return this.cart.reduce((s, r) =>
      s + Math.round(r.quantity * r.price * r.taxPercentage / 100 * 100) / 100, 0);
  }

  get totalAmount(): number {
    const t = this.subtotal + this.gstAmount - this.discountAmount;
    return Math.max(t, 0);
  }

  get totalItemsCount(): number {
    return this.cart.reduce((n, r) => n + r.quantity, 0);
  }

  // ── Checkout ──
  processCheckout(): void {
    if (this.cart.length === 0) return;
    this.lastInvoiceNumber = '';
    this.checkoutConfirmed = false;
    this.checkoutError = '';
    this.showCheckoutModal = true;
  }

  confirmCheckout(): void {
    if (this.isCheckingOut || this.checkoutConfirmed || this.cart.length === 0) return;

    this.isCheckingOut = true;
    this.checkoutError = '';

    const request: CreateOrderRequest = {
      items: this.cart.map(r => ({
        itemId: r.itemId,
        itemName: r.itemName,
        quantity: r.quantity,
        price: r.price,
        taxPercentage: r.taxPercentage,
        total: r.total
      })),
      tableNumber: Number(this.tableNumber) || 0,
      discount: this.discountAmount,
      paymentMode: this.paymentMode,
      customerName: this.customerName || undefined,
      customerPhone: this.customerPhone || undefined,
      customerGstin: this.customerGstin || undefined,
      // Backend order types are written without spaces (e.g. "DineIn"); the UI labels use spaces for readability.
      orderType: this.orderType.replace(/\s+/g, '')
    };

    this.orderService.createOrder(request).subscribe({
      next: (orderId) => {
        this.isCheckingOut = false;
        this.checkoutConfirmed = true;
        this.lastInvoiceNumber = `Order #${orderId}`;
      },
      error: (err) => {
        this.isCheckingOut = false;
        this.checkoutError = err.error?.message || err.error?.Message || 'Failed to save the order. Please try again.';
        console.error('Checkout error:', err);
      }
    });
  }

  // Closing the modal after a *confirmed* checkout is what clears the local cart —
  // the order is already saved server-side by then. Closing before confirming just
  // dismisses the review dialog and leaves the cart untouched.
  closeCheckoutModal(): void {
    const wasConfirmed = this.checkoutConfirmed;
    this.showCheckoutModal = false;
    this.checkoutConfirmed = false;
    this.checkoutError = '';
    if (wasConfirmed) {
      this.clearCart();
    }
  }
}
