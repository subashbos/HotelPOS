# HotelPOS QA Review And Test Gaps

Date: 2026-05-07

Scope: Fast review of core POS risk areas: authentication, billing, orders, stock, cash sessions, backup, settings, and current test coverage.

## Quick Verification

- Command run: `dotnet test HotelPOS.Tests\HotelPOS.Tests.csproj --no-build --nologo`
- Result: 240 passed, 1 failed, 0 skipped, 241 total.
- Failure: `BackupServiceTests.CreateBackupAsync_Creates_Directory_If_Not_Exists`
- Cause: test deletes `AppDomain.CurrentDomain.BaseDirectory\Backups`, which resolves inside `HotelPOS.Tests\bin\Debug\net10.0-windows\Backups`; deletion failed with access denied.

## Highest Priority Loopholes

1. Order save is not atomic with stock deduction.
   - `OrderService.SaveOrderAsync` saves the order first, then deducts stock item by item.
   - If stock deduction fails after the order is saved, the bill exists but inventory may be unchanged or partially updated.
   - Recommended fix: move order save and stock deduction into one transaction/unit of work, or deduct/validate stock before committing the order.

2. Invoice numbers can collide under concurrent billing.
   - `OrderRepository.GetNextInvoiceNumberAsync` reads the latest invoice, increments it, then `AddAsync` saves later.
   - Two checkouts in the same fiscal year can receive the same next number.
   - Recommended fix: add a database unique index on `(FiscalYear, InvoiceNumber)` and allocate invoice numbers inside a transaction/retry loop.

3. Stock can go negative.
   - `ItemService.DeductStockAsync` subtracts quantity without checking available stock.
   - This may be intentional for backorders, but POS inventory usually needs a policy.
   - Recommended fix: reject oversell, allow admin override, or explicitly document negative stock as supported.

4. Update item path bypasses create validation.
   - `AddItemAsync` validates null DTO, empty name, long name, and price > 0.
   - `UpdateItemAsync` does not call the same validation and can accept invalid data.
   - Recommended fix: call `ValidateDto(dto)` at the start of `UpdateItemAsync`.

5. Order input validation is too weak.
   - Orders currently accept zero/negative table numbers, negative discounts, negative quantities, negative prices, and arbitrary payment modes.
   - Recommended fix: validate table number, item quantities, item prices, tax range, discount range, and payment mode whitelist.

6. Backup test uses a shared build output folder.
   - The failing test mutates/deletes a `Backups` directory under the test binary output.
   - Recommended fix: inject backup directory into `BackupService` or isolate the test with a temp directory.

## Missing High-Value Test Cases

Add these first because they cover money, security, and data integrity:

- `OrderService_SaveOrderAsync_WhenStockDeductionFails_DoesNotPersistPartialOrder`
- `OrderRepository_GetNextInvoiceNumberAsync_ConcurrentOrders_DoNotDuplicateInvoiceNumber`
- `HotelDbContext_HasUniqueInvoiceIndex_PerFiscalYear`
- `ItemService_DeductStockAsync_WhenInsufficientStock_RejectsOrAppliesDocumentedPolicy`
- `ItemService_UpdateItemAsync_InvalidDto_ThrowsLikeAddItemAsync`
- `OrderService_SaveOrderAsync_NegativeDiscount_Throws`
- `OrderService_SaveOrderAsync_InvalidPaymentMode_Throws`
- `OrderService_SaveOrderAsync_ZeroOrNegativeQuantity_Throws`
- `OrderService_UpdateOrderAsync_WhenNewStockDeductionFails_RollsBackOldStockReturn`
- `AuthService_AuthenticateAsync_NullOrWhitespaceUsername_DoesNotThrow`
- `UserService_ResetPasswordAsync_NullOrShortPassword_ReturnsValidationError`
- `CashService_OpenSessionAsync_NegativeOpeningBalance_ThrowsAtServiceLayer`
- `CashService_CloseSessionAsync_NegativeActualCash_Throws`
- `BackupService_CreateBackupAsync_UsesIsolatedBackupDirectory`
- `PrintPreview_DefaultPrinterMissing_FallsBackToDialogOrReportsCleanly`

## Documentation Improvements

1. README has encoding artifacts in headings and icons.
   - Several heading icons render as mojibake instead of readable symbols.
   - Recommended fix: re-save as UTF-8 or replace with plain ASCII headings.

2. Add an operations runbook.
   - Startup credentials and forced password change flow.
   - Backup location and restore steps.
   - Printer setup and fallback behavior.
   - Fiscal year and invoice numbering rules.
   - Cash session open/close procedure.

3. Add a QA checklist for release.
   - Billing with discount, GST, payment modes.
   - Edit/delete order stock reconciliation.
   - Printer preview/direct print/default printer.
   - Cash session open/close and sales total.
   - Backup creation and retention.
   - Login lockout and password reset.

4. Add a data integrity section.
   - Explain soft delete behavior for orders.
   - Clarify whether items/categories are hard deleted or should be protected if used in historical bills.
   - Clarify whether negative stock is allowed.

## Suggested Improvement Order

1. Fix the isolated failing backup test.
2. Add validation parity to `ItemService.UpdateItemAsync`.
3. Define and enforce stock policy.
4. Add transactional order save/update with stock reconciliation.
5. Add invoice uniqueness and concurrency tests.
6. Clean README encoding and add the operations runbook.
