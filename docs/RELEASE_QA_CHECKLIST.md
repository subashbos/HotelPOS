# HotelPOS Release QA Checklist

Use this checklist before deploying any new version of HotelPOS.

## 1. Authentication & Security
- [ ] Admin login (admin/admin) works.
- [ ] Password change required on first login.
- [ ] Cashier account has restricted access (cannot see Reports/Settings).
- [ ] Lockout works after 3 failed attempts.

## 2. Billing & Operations
- [ ] Add items to cart (keyboard shortcuts F1/Search).
- [ ] Quantity update (Grid edit and +/- buttons).
- [ ] Discount validation (cannot be negative or > total).
- [ ] Table transfer (Move items from one table to another).
- [ ] Held orders (KOT print and resume).
- [ ] Checkout (F4) saves order and deducts stock.

## 3. Financial Accuracy
- [ ] GST calculation matches manual verification (e.g., 5%, 12%, 18%).
- [ ] Grand total rounding (if enabled) is correct.
- [ ] Invoice number sequence is sequential and per-fiscal year.
- [ ] Duplicate invoice prevention (test concurrent checkouts).

## 4. Hardware & Printing
- [ ] Direct print to default printer (Silent).
- [ ] Print preview renders correctly for both Thermal and A4.
- [ ] Logo appears clearly on receipts.
- [ ] Fallback to dialog if printer is missing.

## 5. Inventory & Management
- [ ] Stock deduction on checkout.
- [ ] Stock return on order deletion.
- [ ] Negative stock prevented (check error message).
- [ ] Bulk item import (CSV/Excel) handles duplicates.

## 6. Cash & Reporting
- [ ] Daily sales total matches Cash Session closing.
- [ ] Export Dashboard to Excel (check all 4 sheets).
- [ ] Category-wise sales pie chart renders correctly.

## 7. System & Persistence
- [ ] Backup created on app close.
- [ ] Manual backup works from settings.
- [ ] Audit logs capture critical actions (Delete Order, Change Price).
