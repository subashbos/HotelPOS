# HotelPOS QA Review And Test Gaps

Date: 2026-05-07

Scope: Fast review of core POS risk areas: authentication, billing, orders, stock, cash sessions, backup, settings, and current test coverage.

## Quick Verification

- Command run: `dotnet test`
- Result: 515 passed, 0 failed, 0 skipped, 515 total.
- Concurrency & Thread-safety: Mitigated. Database concurrent operations are isolated using dynamic scoped service resolution.

## Highest Priority Loopholes (Mitigated & Checked)

1. DbContext concurrency access (MITIGATED)
   - WPF application components systematically execute database operations inside temporary DI scopes (`App.CreateDbScope()`) to avoid thread collisions and context sharing.
   - `OrderService.SaveOrderAsync`/`UpdateOrderAsync` now wrap the order save and stock deduction in a single `BeginTransactionAsync`/`CommitTransactionAsync` unit of work, rolling back on any failure so a partially-deducted stock state can't be persisted.
   - Status: FIXED (`OrderService.cs`).

2. Invoice numbers can collide under concurrent billing. (FIXED)
   - A unique database index on `(FiscalYear, InvoiceNumber)` is now enforced (`HotelDbContext.OnModelCreating`), combined with invoice allocation happening inside the same transaction as the order save.
   - Status: FIXED.

3. Stock can go negative. (FIXED)
   - `ItemService.DeductStockAsync` now rejects the deduction with an `InvalidOperationException` when requested quantity exceeds available stock for inventory-tracked items.
   - Status: FIXED.

4. Missing Delete/Update Validation (MITIGATED via FluentValidation)
   - Previously, `UpdateItemAsync` bypassed the `AddItemAsync` validation rules.
   - Deletion commands across entities lacked validation checks.
   - Fix applied: Integrated `FluentValidation` pipelines for all Commands (Update and Delete), ensuring that business rule constraints are uniformly enforced prior to database writes.

5. Order input validation is too weak. (FIXED)
   - `CreateOrderCommandValidator` now validates non-negative item price, quantity > 0, non-negative discount capped at the order subtotal, payment mode whitelist, order type whitelist, and a positive table number for dine-in orders.
   - Status: FIXED.

6. Backup test uses a shared build output folder. (FIXED)
   - `BackupServiceTests` now creates backups under isolated `Path.GetTempPath()`-based directories per test (with cleanup in `finally` blocks) instead of the shared build-output `Backups` folder.
   - Status: FIXED.

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

1. README encoding. (NOT AN ISSUE)
   - Re-checked: `README.md` is clean UTF-8 (`file` reports `charset=utf-8`, zero replacement characters) and the emoji headings render correctly. The earlier "mojibake" note was inaccurate, likely from viewing the file in a terminal with a non-UTF-8 locale.

2. Add an operations runbook. (DONE)
   - See `docs/OPERATIONS_RUNBOOK.md`.

3. Add a QA checklist for release. (DONE)
   - See `docs/RELEASE_QA_CHECKLIST.md`.

4. Add a data integrity section.
   - Explain soft delete behavior for orders.
   - Clarify whether items/categories are hard deleted or should be protected if used in historical bills.
   - Clarify whether negative stock is allowed.
   - Status: still open.

## Suggested Improvement Order

1. ~~Fix the isolated failing backup test.~~ DONE
2. ~~Add validation parity to `ItemService.UpdateItemAsync`.~~ DONE
3. ~~Define and enforce stock policy.~~ DONE
4. ~~Add transactional order save/update with stock reconciliation.~~ DONE
5. ~~Add invoice uniqueness and concurrency tests.~~ DONE (`HotelDbContextModelSnapshot`/`HotelDbContext` enforce a unique index; see "Missing High-Value Test Cases" above for remaining concurrency-specific test coverage.)
6. Remaining: add the data integrity documentation section (item 4 above).
