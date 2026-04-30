# HotelPOS Knowledge Transfer

This document is for developer handover and operational continuity of the HotelPOS desktop application.

## 1) What This System Does

HotelPOS is a WPF-based point-of-sale app for hotel/restaurant billing with:

- Role-based login (`Admin`, `Manager`, cashier-style restricted role behavior)
- Billing workflow (table selection, cart, checkout, print preview)
- Inventory deduction on order save/update
- Reports and ledgers
- Audit logging
- Settings-managed receipt rendering and printing
- Unified Premium UI (Global DataGrid styling and consistent aesthetics)

Primary startup flow:

1. App bootstraps dependency injection and logging (`App.xaml.cs`)
2. Login window opens
3. On successful auth, a scoped dashboard session is created
4. Dashboard navigates to feature views (Billing, Items, Categories, Ledger, etc.)

## 2) Architecture Overview

The solution follows a layered structure:

- `HotelPOS/`: WPF presentation and composition root
- `HotelPOS.Domain/`: entities and repository contracts
- `HotelPOS.Application/`: business services and use-case orchestration
- `HotelPOS.Persistence/`: EF Core `DbContext`, repositories, migrations
- `HotelPOS.Infrastructure/`: cross-cutting utilities (theme, notifications, backup)
- `HotelPOS.Tests/`: unit/integration-style tests

Dependency direction:

- UI -> Application -> Domain contracts
- Persistence implements Domain contracts
- Infrastructure supports UI/Application concerns

## 3) Key Runtime Components

### Dependency Injection and Lifetimes

Defined in `HotelPOS/App.xaml.cs`.

- `DbContext` is scoped (one per DI scope)
- Repositories and core services are mostly scoped
- Shared utility services such as cart/theme/notification/backup are singleton in current registration
- Login and dashboard sessions are intentionally scoped and disposed on close

### Authentication

Implemented in `HotelPOS.Application/AuthService.cs`.

- PBKDF2 password verification
- In-memory failed login lockout tracking (`5` failed attempts, `5` minute lock window)
- Default seeded admin user exists in `HotelDbContext` seed data

### Ordering and Stock

Implemented in `HotelPOS.Application/OrderService.cs`.

- Creates fiscal-year invoice numbers (`INV/YYYY-YY/####`)
- Calculates subtotal/GST/CGST/SGST/discount/final total
- Persists order via repository
- Deducts stock for each item after save
- Publishes domain action event via MediatR
- Update flow reconciles stock by returning old quantities then applying new quantities

### Data Access

Core data context is in `HotelPOS.Persistence/HotelDbContext.cs`.

- Tables include orders, order items, items, categories, users, system settings, audit logs, cash sessions
- Global decimal precision configured to `(18,2)`
- Includes seed data for admin user and default system settings
- Soft-delete pattern used for orders (`IsDeleted`, `DeletedAt`)

### Receipt + Printing

- Print UI: `HotelPOS/PrintPreviewWindow.xaml.cs`
- Receipt generation: `HotelPOS/ReceiptGenerator.cs`
- Supports thermal and A4 formats
- Uses runtime system settings for branding and tax display behavior
- All DataGrids are standardized using the `PremiumGrid` style defined in `Themes/GlobalStyles.xaml`.

## 4) Database and Configuration

Main configuration file: `HotelPOS/appsettings.json`

- Required key: `ConnectionStrings:DefaultConnection`
- Current development sample points to SQL Server

Important operational note:

- `BackupService` currently uses `SqliteConnection` APIs and depends on database file paths.
- With SQL Server connection strings, this backup logic will typically no-op (or not function as expected).
- Treat automated backup behavior as environment-sensitive; verify and adjust before production rollout.

## 5) Session and Role Behavior

Session state is tracked in `HotelPOS/AppSession.cs`.

- `IsAdmin` and `IsManager` flags control visibility/navigation access in dashboard
- Cashier-like users have admin modules hidden and default to billing screen
- Closing dashboard triggers logout/session disposal and returns to fresh login window

## 6) How to Run and Validate

From repository root:

```powershell
dotnet restore .\HotelPOS\HotelPOS.slnx
dotnet build .\HotelPOS\HotelPOS.slnx
dotnet run --project .\HotelPOS\HotelPOS.csproj
dotnet test .\HotelPOS.Tests\HotelPOS.Tests.csproj
```

Smoke-test checklist for new owner:

1. Login works for admin account
2. Create or select items and checkout successfully
3. Receipt preview opens and can print
4. Order appears in reports/ledger screens
5. Stock changes correctly after checkout/update
6. Audit entries are written for create/update/delete actions

## 7) Frequently Changed Areas

Most likely files to modify during feature work:

- DI registrations and startup behavior: `HotelPOS/App.xaml.cs`
- Billing UX and keyboard shortcuts: `HotelPOS/MainWindow.xaml.cs`, `HotelPOS/Views/BillingView.xaml*`
- Business rules: `HotelPOS.Application/*Service.cs`
- Data shape and migrations: `HotelPOS.Persistence/HotelDbContext.cs`, `HotelPOS.Persistence/Migrations/*`
- Receipt format and compliance details: `HotelPOS/ReceiptGenerator.cs`

## 8) Known Risks / Follow-Up Recommendations

This section is now an execution plan you can track.
Last reviewed: `2026-04-27`

Status legend: `Open` | `In Progress` | `Mitigated` | `Closed`

### Risk 1: Backup strategy does not match active DB provider (MITIGATED)

- **Risk:** `BackupService` originally used SQLite backup APIs, but app configuration often points to SQL Server.
- **Impact:** Production backups could silently fail or be skipped.
- **Priority:** `P0`
- **Status:** `Mitigated` - `BackupService` is provider-aware (SQL Server `BACKUP` + SQLite `BackupDatabase`).
- **Owner:** Backend/Application owner
- **Actions:**
  1. [DONE] Implement provider checks in `BackupService`.
  2. [DONE] Support T-SQL `BACKUP DATABASE` for SQL Server environments.
  3. [DONE] Ensure local directory permissions for backup artifacts.
  4. Perform periodic restore drills to verify `.bak` and `.db` file integrity.
- **Done criteria:** Verified restore drill from latest backup in a non-dev environment.

### Risk 2: Sensitive config in source-controlled `appsettings.json`

- **Risk:** Connection strings and secrets can leak or be reused insecurely.
- **Impact:** Security and compliance exposure.
- **Priority:** `P0`
- **Status:** `In Progress`
- **Owner:** DevOps + App owner
- **Actions:**
  1. [DONE] Replace concrete connection values with placeholders in tracked config (Default config uses LocalDB).
  2. Add environment-specific config (`appsettings.Development.json`, secure prod source).
  3. Use environment variables or secure secret storage for production.
  4. [DONE] Add `.gitignore`/deployment docs for local secret files (`appsettings.Development.json` and `secrets.json` excluded).
- **Done criteria:** No production secrets in repo; deployment docs include secure configuration steps.

### Risk 3: Service lifetime mismatch (singletons vs scoped dependencies)

- **Risk:** Singleton services may indirectly hold or use scoped state incorrectly over time.
- **Impact:** Runtime instability, stale state, difficult-to-debug failures.
- **Priority:** `P1`
- **Status:** `Open`
- **Owner:** Application architect
- **Actions:**
  1. Audit all singleton services and constructor graphs.
  2. Convert to scoped/transient where request/session state is used.
  3. Keep pure stateless utilities as singleton only.
  4. Add integration test that creates/disposes multiple scopes and validates behavior.
- **Done criteria:** Lifetime audit completed and validated in repeated login/logout session tests.

### Risk 6: UI Inconsistency across Modules (MITIGATED)

- **Risk:** Variations in DataGrid styling and row heights made the app feel unpolished.
- **Impact:** Poor user perception of the "Premium" product level.
- **Priority:** `P2`
- **Status:** `Mitigated`
- **Owner:** Frontend/UI owner
- **Actions:**
  1. [DONE] Define `PremiumGrid` in `GlobalStyles.xaml`.
  2. [DONE] Migrate all views (`Dashboard`, `Billing`, `Ledger`, `Journal`, `Users`, `Category`, `Audit`, `Session`) to use `PremiumGrid`.
  3. [DONE] Standardize row height to `50px` for better touch and visual clarity.
- **Done criteria:** All tables in the application share identical header colors, row hover effects, and typography.

### Risk 4: Targeted automated coverage gaps

- **Risk:** Edge cases in checkout, stock reconciliation, and invoice numbering can ship undetected.
- **Impact:** Financial and operational errors.
- **Priority:** `P1`
- **Status:** `In Progress`
- **Owner:** QA + Backend owner
- **Actions:**
  1. Add tests for fiscal year invoice transitions and sequence format (rollover logic).
  2. Add backup behavior tests validated by DB provider.
  3. Implement end-to-end soft-delete filtering verification in repositories.
  4. [DONE] Add tests for `UpdateOrderAsync` stock reconciliation (rollback/re-apply).
- **Done criteria:** Critical order flow tests pass in CI; failures block release.

### Risk 5: Seeded default admin credential policy (MITIGATED)

- **Risk:** Default credential assumptions can persist into deployed environments.
- **Impact:** Unauthorized access risk.
- **Priority:** `P0`
- **Status:** `Mitigated`
- **Owner:** Security + App owner
- **Actions:**
  1. [DONE] Ensure seeded credentials are development-only.
  2. [DONE] Enforce password reset on first successful login for seeded/admin bootstrap accounts (`MustChangePassword` flag added).
  3. [DONE] Add minimum password policy and lockout/user feedback documentation.
  4. Add deployment checklist step: rotate/disable bootstrap user post go-live.
- **Done criteria:** No default credential remains active in staging/production.

### Suggested rollout timeline

- **Week 1 (must-do):** Risk 1, Risk 2, Risk 5
- **Week 2:** Risk 3
- **Week 3:** Risk 4 + CI quality gate tightening

## 9) Handover Checklist (People/Process)

- Confirm who owns database schema/migrations approvals.
- Confirm release process (build artifacts, installer/publish profile, rollback).
- Confirm printer setup expectations per site (thermal/A4 defaults).
- Confirm audit retention and backup retention policy.
- Capture support contact and escalation path for billing outages.

## 10) Quick Reference

- Startup + DI: `HotelPOS/App.xaml.cs`
- Login/session scope: `HotelPOS/LoginWindow.xaml.cs`
- Dashboard navigation: `HotelPOS/DashboardWindow.xaml.cs`
- Billing workflow: `HotelPOS/MainWindow.xaml.cs`
- Order business logic: `HotelPOS.Application/OrderService.cs`
- Auth logic: `HotelPOS.Application/AuthService.cs`
- Data context: `HotelPOS.Persistence/HotelDbContext.cs`
- Receipt rendering: `HotelPOS/ReceiptGenerator.cs`

