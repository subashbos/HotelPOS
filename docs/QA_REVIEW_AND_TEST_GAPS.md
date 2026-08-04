# HotelPOS QA Review And Test Gaps

Date: 2026-05-07

Scope: Fast review of core POS risk areas: authentication, billing, orders, stock, cash sessions, backup, settings, and current test coverage.

## Quick Verification

- Command run: `dotnet test HotelPOS.sln --configuration Release`
- Result (as of 2026-07-23): 1,196 passed, 0 failed, 0 skipped, 1,196 total (423 in `HotelPOS.Tests`, 773 in `HotelPOS.Api.Tests`). The suite has grown substantially since this document's original 2026-05-07 review (515 tests then), largely from the HR module and API test coverage — see [HUMAN_RESOURCES_DEEP_DIVE.md](HUMAN_RESOURCES_DEEP_DIVE.md) §8.
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

6. `ApiUserContext.CurrentUserId` was always `null` for every real login. (FIXED 2026-08-04)
   - Found via the new `UsersHttpTests.cs` HTTP-level integration tests, not by inspection — a mocked-service unit test mocks `IUserContext` directly and can't catch this class of bug.
   - Root cause: `ApiUserContext.CurrentUserId` reads the JWT's `sub` claim via `JwtRegisteredClaimNames.Sub`, but ASP.NET Core's default JWT bearer inbound claim mapping silently renames `sub` → `ClaimTypes.NameIdentifier` before the claim reaches `HttpContext.User`. `AuthController` mints real login tokens with that same raw `sub` claim name, so this was never test-harness-specific — it broke every real logged-in user.
   - Impact: `UsersController`'s self-delete guard never fired (an admin could delete their own account), and self-service password/2FA changes always fell through to requiring the `Settings` permission instead of the intended "you can change your own" bypass (a non-admin resetting their own password incorrectly got `403 Forbidden`). Likely also affected `EssController`'s "own payslips" access and `PayrollService.GetPayslipsByEmployeeAsync`'s self-or-permission check.
   - Fix: `options.MapInboundClaims = false` on the JWT bearer config in `Program.cs`, so claims keep the exact names both `AuthController` mints and `ApiUserContext` reads. All 6 claim read/write sites in `src/API`/`src/Application` were audited first — `Role` claims already use the long-form `ClaimTypes.Role` on both ends, so this only fixes `Sub`/`UniqueName` resolution with no other regression risk.
   - Status: FIXED (`Program.cs`).

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

## CI Coverage Gate

- The WPF test step (`dotnet.yml` → `Test (WPF)`) has enforced `-p:Threshold=80` (line coverage) for a while, but the `Test (API/backend)` step — covering `Application`, `Infrastructure`, and `API`, the most business-critical layer — collected coverage without ever gating the build on it.
- Root cause of why a naive threshold couldn't just be copied over: `HotelPOS.Infrastructure.Persistence.Migrations` is ~71k lines of EF Core-generated scaffolding (vs. ~3.1k lines of hand-written `Persistence` code), and it's essentially 0% exercised by tests since nothing runs migration `Up`/`Down` methods directly. That dragged the unweighted `Infrastructure` module down to ~1.7% line coverage and the overall `Test (API/backend)` total down to ~8.5%, even though `API` (~82%), `Application` (~79%), and `Domain` (~90%) were already healthy.
- Fix (2026-08-04): excluded the `Migrations` namespace from Coverlet instrumentation (`-p:Exclude="[HotelPOS.Infrastructure]HotelPOS.Infrastructure.Persistence.Migrations.*"`) so coverage reflects hand-written code, and added a `-p:Threshold` gate (`ThresholdType=line`, `ThresholdStat=total`).
- Initial value was `60`, deliberately conservative since it was estimated (no local .NET toolchain was available to measure directly). The first real CI run under the new instrumentation reported: `HotelPOS.Api` 82.07%, `HotelPOS.Application` 79.18%, `HotelPOS.Domain` 90%, `HotelPOS.Infrastructure` 72.48% (up from ~1.7% pre-exclusion) — weighted total **78.98%** line coverage. Threshold was raised to `75` the same day to reflect that, leaving a small buffer under the observed total while landing close to the WPF project's 80% bar. Continue ratcheting toward 80 as coverage improves.

## Suggested Improvement Order

1. ~~Fix the isolated failing backup test.~~ DONE
2. ~~Add validation parity to `ItemService.UpdateItemAsync`.~~ DONE
3. ~~Define and enforce stock policy.~~ DONE
4. ~~Add transactional order save/update with stock reconciliation.~~ DONE
5. ~~Add invoice uniqueness and concurrency tests.~~ DONE (`HotelDbContextModelSnapshot`/`HotelDbContext` enforce a unique index; see "Missing High-Value Test Cases" above for remaining concurrency-specific test coverage.)
6. ~~Enforce a coverage gate on the API/backend test step in CI.~~ DONE (see "CI Coverage Gate" above) — follow-up: raise the threshold once real numbers are observed.
7. ~~Add UserService/SettingService coverage for the previously-untested legacy code paths.~~ DONE (2026-08-04) — `UserServiceContactSettingsTests.cs` covers `SetTwoFactorAsync`/`SetEmailAsync` (not-found, mutation, disable-clears-secret, authorization); `SettingServiceAuthorizationTests.cs` covers `SaveSettingsAsync` authorization enforcement on both constructor paths and the previously-untested `GetSettingsAsync` mediator-DI path.
8. ~~Add real HTTP-level integration tests for controllers that only had mocked-service tests.~~ IN PROGRESS — two batches landed so far, following the pattern `RoleAuthorizationTests.cs` established (`CustomWebApplicationFactory`: real routing, model binding, JWT auth, RBAC, and the mediator/EF pipeline against an isolated in-memory SQLite DB). These catch classes of bug mocked-service controller tests structurally can't — see item 6 above for a real production bug (`ApiUserContext.CurrentUserId`) found this way.
   - Batch 1 (2026-08-04): `CashSessionsHttpTests.cs`, `SettingsHttpTests.cs`, `UsersHttpTests.cs`, `PayrollHttpTests.cs` — `CashSessionsController`, `SettingsController`, `UsersController`, `PayrollController`. Notable: `SettingsController.GetSettings`'s permission-conditional field visibility.
   - Batch 2 (2026-08-04): `CategoriesHttpTests.cs`, `SuppliersHttpTests.cs`, `TablesHttpTests.cs`, `UnitOfMeasurementsHttpTests.cs`, `CustomersHttpTests.cs` — `CategoriesController`, `SuppliersController`, `TablesController`, `UnitOfMeasurementsController`, `CustomersController`. Notable: `CustomersController` has an inverted permission split versus every other master-data controller — the seeded Cashier role is actually granted `Customers` (create/update) but not `CustomerManagement` (delete), the opposite of the usual "Cashier gets nothing" pattern, and the unauthenticated-role fallback in `AuthorizationService` would incorrectly deny it if a real seeded `User` row isn't used — `CustomersHttpTests.cs`'s doc comment explains this precisely so it isn't "fixed" into the wrong behavior later.
   - Remaining controllers without real HTTP-level tests: `AttendanceController`, `AuditController`, `EmployeesController`, `EssController`, `ExpensesController`, `LeaveController`, `PurchasesController`, `RolesController` (partially covered via `RoleAuthorizationTests.cs`), `TdsController`.
9. Remaining: add the data integrity documentation section (item 4 above).
