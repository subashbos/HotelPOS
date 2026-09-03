# HotelPOS QA Review And Test Gaps

Date: 2026-05-07

Scope: Fast review of core POS risk areas: authentication, billing, orders, stock, cash sessions, backup, settings, and current test coverage.

## Quick Verification

- Command run: `dotnet test HotelPOS.sln --configuration Release`
- Result (as of 2026-09-02): 2,076 passed, 0 failed, 0 skipped, 2,076 total (539 in `HotelPOS.Tests`, 1,537 in `HotelPOS.Api.Tests`). The suite has grown substantially since this document's original 2026-05-07 review (515 tests then) and its 2026-07-23 count (1,196), largely from the HR module and API test coverage — see [HUMAN_RESOURCES_DEEP_DIVE.md](HUMAN_RESOURCES_DEEP_DIVE.md) §8.
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

7. `POST /api/expenses` always returned `Id: 0` for the newly-created expense. (FIXED 2026-08-04)
   - Found via the new `ExpensesHttpTests.cs` HTTP-level integration test (`DeleteExpense_CashierToken_ReturnsForbidden`), which create-then-deletes using the ID from the create response — it got `400 Bad Request` ("Invalid expense ID") instead of the expected `403 Forbidden`, because the ID it tried to delete was `0`.
   - Root cause: `ExpenseService.SaveExpenseAsync`'s mediator-DI path calls `_mediator.Send(new SaveExpenseCommand(dto))`, which returns the real generated ID as `id` — but `SaveExpenseCommandHandler.Handle` constructs a **new** `Expense` entity internally (`var expense = new Expense {...}`), disconnected from the controller's local `expense` object. The service captured `id` in a local variable (used only for the audit event) and never wrote it back onto the caller's `expense.Id`, so `ExpensesController.CreateExpense`'s `CreatedAtAction(..., new { id = expense.Id }, ...)` — both the `Location` header and the JSON body — always reported `Id: 0` in production, for every expense ever created through the real app.
   - Contrast: `PurchaseService`/`SavePurchaseCommand` pass the actual domain entity through the whole mediator pipeline (not a DTO), so EF's identity generation mutates the same object the controller holds — that class of service does **not** have this bug. Worth checking the same "does the mediator path return the new ID to the caller by DTO or by the entity being the same reference?" question for any other service before assuming it's fine.
   - Fix: `expense.Id = id;` right after the mediator call, in `ExpenseService.cs`.
   - Status: FIXED (`ExpenseService.cs`).

8. `POST /api/purchases` with an invalid `SupplierId` (e.g. `0`) returned `500` instead of `400`. (FIXED 2026-08-04)
   - Found via the new `PurchasesHttpTests.cs` — `CreatePurchase_InvalidSupplierId_ReturnsBadRequest` got an unhandled `DbUpdateException` (FK violation on `Purchase.SupplierId → Suppliers.Id`), not the clean `FluentValidation.ValidationException` expected. Direct evidence from `_env.IsDevelopment()`'s stack trace in the response body confirmed the request reached `PurchaseRepository.AddAsync`/`SaveChangesAsync` — i.e. validation never ran.
   - Root cause: `SavePurchaseCommand`/`UpdatePurchaseCommand` are void (`IRequest`, not `IRequest<TResponse>`) commands with real FluentValidation validators (`SavePurchaseCommandValidator`, `UpdatePurchaseCommandValidator`) — and for these two specifically, the generic `ValidationBehavior<TRequest,TResponse>` MediatR pipeline behavior did not reject the invalid command before `Handle` ran. A directly-instantiated `SavePurchaseCommandValidator().TestValidate(...)` unit test (`ValidatorTests.cs`) already proved the validator's *logic* is correct in isolation — the gap is specifically in the pipeline actually applying it for this request shape. **Not yet root-caused why** — every other validated command exercised so far uses `IRequest<TResponse>` (e.g. `SaveExpenseCommand : IRequest<int>`), and none of the *other* void commands' validators (several `DeleteXCommandValidator`s exist) were ever exercised through a path where the validator was the sole gate — their controllers all have a redundant `if (id <= 0)` check first, which is exactly why this went unnoticed until now. Worth a follow-up investigation into whether this is a systemic gap affecting every void-`IRequest` command with a validator, not just Purchases.
   - Fix applied (narrow, not a MediatR pipeline fix): `PurchaseService.SavePurchaseAsync`/`UpdatePurchaseAsync`'s mediator branches now call the existing `ValidatePurchase(purchase)` helper (already used by the legacy constructor path) before `_mediator.Send(...)`, mirroring what the legacy path already did. Verified safe against all three existing test suites that touch `SavePurchaseCommandHandler`/`UpdatePurchaseCommandHandler` directly (`SavePurchaseCommandHandlerTests.cs`, `UpdatePurchaseCommandHandlerTests.cs`, `GenericPermissionEnforcementTests.cs`) since `ValidatePurchase` isn't in the handler itself — those tests construct `Purchase` objects that don't set `GrandTotal`/`PurchaseDate` and would have broken against the stricter FluentValidation rules had the fix been placed in the handler instead.
   - Status: FIXED (`PurchaseService.cs`) — narrow fix; the underlying pipeline-behavior gap for void commands is still open, see above.
   - Confirmed the same gap for `UpdateOrderCommand` (also void `IRequest`): `PUT /api/orders/{id}` with a negative `Discount` returned `204 NoContent` instead of `400 BadRequest` — `UpdateOrderCommandValidator` never ran. (FIXED 2026-09-03)
     - Fix: same narrow pattern as Purchases — `OrderService` now takes an injected `IValidator<UpdateOrderCommand>` (defaulting to `new UpdateOrderCommandValidator()`) and validates directly at the top of `UpdateOrderInternalAsync`, replacing the old ad-hoc `if (order.Items == null || order.Items.Count == 0)` check with the full validator (empty items, negative price, non-positive quantity, negative discount, invalid payment mode/order type, invalid table number for DineIn).
     - This surfaced that several existing `UpdateOrderAsync`/`UpdateOrderInternalAsync` unit tests built `Order` objects that never set `TableNumber`, relying on the fact that validation never actually ran — fixed by adding `TableNumber` to those test fixtures (`OrderServiceUpdateTests.cs`, `StockReconciliationTests.cs`, `BillingEditTests.cs`, `OrderServiceBomWiringTests.cs`, `OrderServiceTests.cs`, `PhaseTests.cs`, `SaveUpdateRegressionTests.cs`).
     - Regression test: `OrdersHttpTests.UpdateOrder_NegativeDiscount_ReturnsBadRequest`.
     - The other ~17 void commands with validators (Category, Estimation, Expense, Reservation, Role, Settings, Supplier, Table, UnitOfMeasurement, User) are still unverified — each needs the same check-and-patch treatment individually until the pipeline-behavior root cause is fixed.

9. Backup test uses a shared build output folder. (FIXED)
   - `BackupServiceTests` now creates backups under isolated `Path.GetTempPath()`-based directories per test (with cleanup in `finally` blocks) instead of the shared build-output `Backups` folder.
   - Status: FIXED.

## Missing High-Value Test Cases

Add these first because they cover money, security, and data integrity:

- `OrderService_SaveOrderAsync_WhenStockDeductionFails_DoesNotPersistPartialOrder` — DONE (`OrderServiceTests.cs`).
- `OrderRepository_GetNextInvoiceNumberAsync_ConcurrentOrders_DoNotDuplicateInvoiceNumber` — DONE (`ConcurrencyAndUniquenessTests.cs`).
- `HotelDbContext_HasUniqueInvoiceIndex_PerFiscalYear` — still open.
- `ItemService_DeductStockAsync_WhenInsufficientStock_RejectsOrAppliesDocumentedPolicy` — DONE (`DeductStockAsync_InsufficientStock_ThrowsInvalidOperationException` in `ItemServiceLoopholeTests.cs`).
- `ItemService_UpdateItemAsync_InvalidDto_ThrowsLikeAddItemAsync` — DONE (`ItemServiceUpdateTests.cs`: zero price and negative tax percentage both throw `ArgumentException` without touching the repository, mirroring `AddItemAsync`).
- `OrderService_SaveOrderAsync_NegativeDiscount_Throws` — DONE (`OrderServiceLoopholeTests.cs`).
- `OrderService_SaveOrderAsync_InvalidPaymentMode_Throws` — DONE (`OrderServiceLoopholeTests.cs`).
- `OrderService_SaveOrderAsync_ZeroOrNegativeQuantity_Throws` — DONE (`SaveOrderAsync_ZeroOrNegativeQuantity_ThrowsArgumentException` in `OrderServiceLoopholeTests.cs`).
- `OrderService_UpdateOrderAsync_WhenNewStockDeductionFails_RollsBackOldStockReturn` — still open.
- `AuthService_AuthenticateAsync_NullOrWhitespaceUsername_DoesNotThrow` — DONE (`AuthServiceTests.cs`: verifies null/empty/whitespace usernames return `null` without throwing and without querying the user repository).
- `UserService_ResetPasswordAsync_NullOrShortPassword_ReturnsValidationError` — mostly covered (`UserServiceLoopholeTests.cs` covers null/empty); a below-minimum-length non-empty password case is still open.
- `CashService_OpenSessionAsync_NegativeOpeningBalance_ThrowsAtServiceLayer` — DONE (`CashServiceTests.cs`).
- `CashService_CloseSessionAsync_NegativeActualCash_Throws` — DONE (`CashServiceTests.cs`).
- `BackupService_CreateBackupAsync_UsesIsolatedBackupDirectory` — DONE (covered by the isolated-directory fix noted above).
- `PrintPreview_DefaultPrinterMissing_FallsBackToDialogOrReportsCleanly` — DONE (`WpfServicesTests.cs`).

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
8. ~~Add real HTTP-level integration tests for controllers that only had mocked-service tests.~~ DONE (2026-08-04) — four batches, following the pattern `RoleAuthorizationTests.cs` established (`CustomWebApplicationFactory`: real routing, model binding, JWT auth, RBAC, and the mediator/EF pipeline against an isolated in-memory SQLite DB). These catch classes of bug mocked-service controller tests structurally can't — see items 6-8 above for the three real production bugs found this way (`ApiUserContext.CurrentUserId`, `ExpenseService`'s dropped ID, `PurchaseService`'s validation bypass).
   - Batch 1 (2026-08-04): `CashSessionsHttpTests.cs`, `SettingsHttpTests.cs`, `UsersHttpTests.cs`, `PayrollHttpTests.cs` — `CashSessionsController`, `SettingsController`, `UsersController`, `PayrollController`. Notable: `SettingsController.GetSettings`'s permission-conditional field visibility.
   - Batch 2 (2026-08-04): `CategoriesHttpTests.cs`, `SuppliersHttpTests.cs`, `TablesHttpTests.cs`, `UnitOfMeasurementsHttpTests.cs`, `CustomersHttpTests.cs` — `CategoriesController`, `SuppliersController`, `TablesController`, `UnitOfMeasurementsController`, `CustomersController`. Notable: `CustomersController` has an inverted permission split versus every other master-data controller — the seeded Cashier role is actually granted `Customers` (create/update) but not `CustomerManagement` (delete), the opposite of the usual "Cashier gets nothing" pattern, and the unauthenticated-role fallback in `AuthorizationService` would incorrectly deny it if a real seeded `User` row isn't used — `CustomersHttpTests.cs`'s doc comment explains this precisely so it isn't "fixed" into the wrong behavior later.
   - Batch 3 (2026-08-04): `ExpensesHttpTests.cs`, `PurchasesHttpTests.cs`, `AuditHttpTests.cs`, `TdsHttpTests.cs`, `RolesHttpTests.cs` — `ExpensesController`, `PurchasesController`, `AuditController`, `TdsController`, `RolesController` (its own CRUD, distinct from `RoleAuthorizationTests.cs`'s coverage of how Role/RolePermission rows drive enforcement elsewhere). Notable: two more "delete is idempotent, not 404" endpoints found this way — `PurchasesController.DeletePurchase` (`DeletePurchaseCommandHandler` treats a missing purchase as a no-op, same convention as `OrderService.DeleteOrderInternalAsync`) and `RolesController.DeleteRole` (`RoleRepository.DeleteRoleAsync` silently no-ops for a missing role — the controller's `KeyNotFoundException` catch for that action is dead code, since nothing in that path ever throws it). Also: `TdsController`'s slab validation (contiguous bands, top slab must have no upper bound) needed a deliberately-constructed valid payload to test past the happy path — see `TdsHttpTests.cs`'s `ValidRuleSetPayload()`. This batch also caught a real production bug — see item 7 above (`ExpensesController` always returning `Id: 0` on create) — and revealed that `PurchaseItem.ItemId` is a real required FK to `Items` (via the `Item?` navigation property), so purchase-creating tests must seed a real `Item` first rather than using an arbitrary ID.
   - Batch 4 (2026-08-04): `AttendanceHttpTests.cs`, `EmployeesHttpTests.cs`, `EssHttpTests.cs`, `LeaveHttpTests.cs` — `AttendanceController`, `EmployeesController`, `EssController`, `LeaveController`. This was the last batch — **all controllers now have real HTTP-level integration test coverage**, closing out this improvement item entirely. Notable: `EssHttpTests.cs` doubles as an end-to-end confirmation of the `CurrentUserId` JWT claim-mapping fix (item 6 above) — every ESS action resolves the caller's own `Employee` via `ApiUserContext.CurrentUserId → Employee.UserId`, including a direct security test (`ApplyLeave_IgnoresClientSuppliedEmployeeId_UsesOwnResolvedEmployeeId`) proving a caller can't apply leave on another employee's behalf by supplying a different `EmployeeId` in the request body. `Attendance.EmployeeId` and `LeaveType` needed the same "seed real data first" treatment as `PurchaseItem.ItemId` (batch 3) — no `LeaveType` is seeded by default and there's no API to create one, so `LeaveHttpTests.cs`/`EssHttpTests.cs` seed it directly via the DB.
9. Remaining: add the data integrity documentation section (item 4 above).
