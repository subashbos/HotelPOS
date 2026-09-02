# Human Resources Module — Deep Dive

This document is a technical deep dive into the Human Resources (HR) capabilities of
HotelPOS: Employee master data, Attendance, Leave management, and Payroll. It covers
architecture, data model, business rules, API surface, UI wiring, security/permissions,
test coverage, and known gaps.

## 1. Overview

HR was introduced in migration `20260712154611_AddHumanResourcesModule` and follows the
same Clean Architecture layering as the rest of HotelPOS:

```
Domain        -> Entities, enums/constants (no dependencies)
Application   -> UseCases (services), Interfaces, DTOs, Validators, AutoMapper profiles
Infrastructure-> EF Core repositories, DbContext configuration
API           -> ASP.NET Core controllers (JWT-secured REST endpoints)
HotelPOS      -> WPF desktop views/viewmodels
HotelPOS.Client -> Angular web views/components (added 2026-07-18)
```

Four sub-areas exist, each with its own Service/Repository pair:

| Area        | Service              | Repository              | Entities                                  |
|-------------|-----------------------|--------------------------|--------------------------------------------|
| Employees   | `EmployeeService`      | `EmployeeRepository`     | `Employee`, `Department`, `Designation`    |
| Attendance  | `AttendanceService`    | `AttendanceRepository`   | `Attendance`                               |
| Leave       | `LeaveService`         | `LeaveRepository`        | `LeaveType`, `LeaveBalance`, `LeaveRequest`|
| Payroll     | `PayrollService`       | `PayrollRepository`      | `SalaryStructure`, `PayrollRun`, `Payslip` |

## 2. Domain Model

### Employee (`src/Domain/Entities/Employee.cs`)
Core master record. Notable fields:
- `EmployeeCode` — unique, auto-generated as `EMP0001`, `EMP0002`, ... by
  `EmployeeService.GenerateNextEmployeeCodeAsync()` when left blank.
- `DepartmentId` / `DesignationId` — FKs to lookup tables `Department`, `Designation`.
- `ReportingManagerId` — **self-referencing FK** to `Employee`, enabling an org hierarchy
  (no org-chart UI consumes this yet).
- `UserId` — optional link to a login `User`, connecting HR identity to system auth.
- India-specific compliance fields stored as plain columns: `Pan`, `Aadhaar`, `Uan`,
  `EsicNumber`, plus bank details (`BankName`, `BankAccountNumber`, `BankIfsc`).
- `Status` (`EmployeeStatuses`: Active/OnLeave/Suspended/Resigned/Terminated) and
  `EmploymentType` (`EmploymentTypes`: Permanent/Probation/Contract/PartTime).

### Attendance (`Attendance.cs`)
One record per employee per day (`EmployeeId` + `Date`), with `CheckInTime`/`CheckOutTime`
(`TimeSpan?`), computed `WorkedHours`, and `Status` (`AttendanceStatuses`: Present, Absent,
HalfDay, OnLeave, Holiday, WeekOff).

### Leave (`LeaveType`, `LeaveBalance`, `LeaveRequest`)
- `LeaveType` defines `Code` (CL/SL/EL/ML/LWP via `LeaveTypeCodes`), `AnnualQuota`,
  `IsPaid`, `CarryForwardAllowed`.
- `LeaveBalance` is per employee/type/year, with `AvailableDays` as a computed
  (`[NotMapped]`) property: `EntitledDays - UsedDays`.
- `LeaveRequest` tracks the approval workflow: `Status` (Pending/Approved/Rejected/
  Cancelled), `ApprovedByEmployeeId`, `ActionedOn`, `RejectionReason`.

### Payroll (`SalaryStructure`, `PayrollRun`, `Payslip`)
- `SalaryStructure` is time-sliced per employee (`EffectiveFrom`/`EffectiveTo`) with
  earning components (Basic, HRA, DA, Conveyance, Medical, Special) and statutory
  applicability flags (`PfApplicable`, `EsiApplicable`, `ProfessionalTaxApplicable`).
  `GrossMonthly` is a computed sum of all components.
- `PayrollRun` is one per Month/Year (`Draft` → `Processed` → `Paid`), owning a
  collection of `Payslip`s.
- `Payslip` captures the computed breakdown per employee per run: gross earnings,
  paid/LOP days, PF (employee+employer), ESI (employee+employer), Professional Tax,
  TDS, and `NetPay`.

## 3. Business Logic

### Employee lifecycle (`EmployeeService`)
- Trims code/name fields; auto-generates `EmployeeCode` if blank.
- Validates via FluentValidation (`EmployeeValidator`): required code/first name/DOJ,
  DOE ≥ DOJ, phone digit-count 10–15, email regex, **Indian PAN format**
  (`AAAAA9999A`), **12-digit Aadhaar**, and **IFSC format** (`AAAA0999999`).
- Enforces `EmployeeCode` uniqueness (`ExistsByCodeAsync`) before insert/update.
- Delete requires the employee to exist first (`KeyNotFoundException` otherwise).

### Attendance (`AttendanceService`)
- `MarkAttendanceAsync` is an **upsert**: if a record already exists for
  employee+date it updates in place rather than duplicating.
- `WorkedHours` is derived automatically from check-in/check-out when both are present
  (rounded to 2 decimals, floored at 0 for negative spans).
- Validates `Status` against the `AttendanceStatuses.All` allow-list.

### Leave (`LeaveService`)
- **Balances are lazily initialized**: the first time a leave type is touched for an
  employee/year, a `LeaveBalance` row is created with `EntitledDays = LeaveType.AnnualQuota`.
- `ApplyLeaveAsync`: computes `TotalDays` from the date range if not supplied, validates,
  and — for any leave type other than **LWP (Leave Without Pay)** — checks sufficient
  balance *before* allowing submission, then **reserves** the requested days on
  `LeaveBalance.PendingDays` immediately (see below).
- `ApproveLeaveAsync` converts the hold into committed usage
  (`PendingDays -= TotalDays`, `UsedDays += TotalDays`); `RejectLeaveAsync` releases it
  (`PendingDays -= TotalDays`). `AvailableDays = EntitledDays - UsedDays - PendingDays`,
  so a second, overlapping application can no longer pass the balance check while the
  first is still pending — the reservation closes the race described in the original
  version of this document (fixed; see §7 for history).
- Only `Pending` requests can be approved or rejected (guarded with
  `InvalidOperationException`).

### Payroll (`PayrollService`)
Computation logic lives in `CalculatePayslip`, driven by Indian statutory constants in
`IndianStatutoryDefaults` (`src/Domain/Common/Constants/AppConstants.cs`):

| Parameter | Value |
|---|---|
| PF employee/employer rate | 12% / 12% |
| PF wage ceiling | ₹15,000/month |
| ESI employee/employer rate | 0.75% / 3.25% |
| ESI wage threshold | ₹21,000 gross/month |
| Professional Tax threshold | ₹15,000 gross/month |
| Professional Tax amount | ₹200 (flat, when above threshold) |
| TDS | Auto-computed via `TdsCalculator.CalculateMonthlyTds` against `TdsConfig`/`TdsSlab` (new tax regime only; see §7 for history) |

`RunPayrollAsync`:
1. Rejects if a run already exists for month/year.
2. Iterates only `Active` employees.
3. Skips employees with no `SalaryStructure` on file as of month-end.
4. LOP (loss of pay) days = `Absent` days + `0.5 × HalfDay` days from the `Attendance`
   table for that month — **note**: leave-approved days use the `OnLeave` attendance
   status, which is *not* counted toward LOP, so approved paid leave doesn't reduce pay
   (consistent with leave balances already gating the leave itself).
5. Proration is `paidDays / workingDays`, where `workingDays` is per-employee calendar
   days in the month **minus** that employee's own `WeekOff`/`Holiday` attendance rows
   for the month (floored at 1) — so a weekly-off pattern tracked in `Attendance`
   correctly shrinks the payable-day denominator instead of treating every calendar day
   as payable (fixed; see §7 for history).
6. `MarkRunAsPaidAsync` flips the run and all its payslips to `Paid`, stamping `PaidOn`;
   only a `Processed` run can be marked paid.

## 4. API Surface (all under `[Authorize]`, JWT required)

| Controller | Route prefix (via `BaseApiController`) | Notable role restrictions |
|---|---|---|
| `EmployeesController` | `/api/employees` | Create/Update: Admin, Manager. Delete: Admin only. |
| `AttendanceController` | `/api/attendance` | Mark/Delete: Admin, Manager. |
| `LeaveController` | `/api/leave` | Approve/Reject: Admin, Manager. Apply/list: any authenticated user. |
| `PayrollController` | `/api/payroll` | Class-level `[Authorize]` only — no role restrictions on the controller itself. View (salary structures/runs/payslips): `PermissionModules.HrPayroll`. Save salary structure / run / mark-paid / void: `PermissionModules.HrPayrollRun`, enforced in `PayrollService` via `IAuthorizationService.EnsureEditPermission`, not a `[Authorize(Roles=...)]` attribute. |

All write endpoints funnel service-layer `ArgumentException`/`InvalidOperationException`
into `400 BadRequest`, and `KeyNotFoundException` into `404 NotFound`, keeping HTTP
concerns out of the Application layer.

## 5. Permissions & UI Wiring

- HR nav visibility in the WPF shell (`DashboardWindow.xaml.cs`) is gated by four
  separate permission modules — `PermissionModules.HrEmployees`, `HrAttendance`,
  `HrLeave`, `HrPayroll` — one per screen, so a role can be granted (for example)
  Attendance and Leave without also seeing Payroll (fixed; see §7 for history). This
  replaced the original single `HumanResources` flag; existing Admin/Cashier role rows
  were migrated to the four new module rows via
  `20260716090000_SplitHumanResourcesPermission`. Action-level restrictions (e.g. who can
  *run* payroll vs. just view it) still live only at the API layer via
  `[Authorize(Roles = ...)]`.
- Views are lazily created and cached per dashboard session
  (`_cachedEmployees`, `_cachedAttendance`, etc.), consistent with how other modules
  (Billing, Inventory) are wired.
- **The Angular web client (`HotelPOS.Client`) now has HR UI**, added 2026-07-18 (after
  this document's original writing): `EmployeesComponent`, `AttendanceComponent`,
  `LeaveComponent`, and `PayrollComponent`, routed at `/admin/{employees,attendance,leave,payroll}`
  in `app.routes.ts`. HR is no longer WPF-desktop-only; both clients consume the same
  REST API.

## 6. Data Layer

- `HotelDbContext` (`src/Infrastructure/Persistence/HotelDbContext.cs`) registers all HR
  `DbSet`s; schema was introduced via `20260712154611_AddHumanResourcesModule` with
  standard identity PKs and FK relationships (Departments, Designations, LeaveTypes,
  Employees, Attendances, LeaveBalances, LeaveRequests, SalaryStructures, PayrollRuns,
  Payslips).
- Repositories are thin pass-throughs over EF Core (`Add`/`Update` + `SaveChangesAsync`)
  with `Include()` used where navigation properties are needed for display (e.g.
  Payslip → PayrollRun/Employee).
- In `PayrollService.RunPayrollAsync`, `payslip.PayrollRunId = run.Id` is assigned before
  the run itself is persisted (so it's `0` at that point) — harmless because
  `PayrollRepository.AddRunAsync` adds the parent `PayrollRun` with its `Payslips`
  navigation collection populated, and EF Core's relationship fix-up sets the real FK on
  save. The explicit assignment is redundant but not a bug.

## 7. Observations & Gaps (for future work)

Fixed since the original version of this document:

1. ~~**Leave balance is reserved at approval, not at application.**~~ **Fixed.**
   `LeaveBalance` now has a `PendingDays` column; `ApplyLeaveAsync` reserves the
   requested days on submission (`AvailableDays = EntitledDays - UsedDays - PendingDays`),
   `ApproveLeaveAsync` converts the hold to `UsedDays`, and `RejectLeaveAsync` releases
   it. A second overlapping application can no longer pass the balance check while the
   first is still pending. Schema change shipped in migration
   `20260716093000_AddLeaveBalancePendingDays`.
2. ~~**Payroll proration uses calendar days, not working days.**~~ **Fixed.**
   `RunPayrollAsync` now computes `workingDays` per employee as calendar days in the
   month minus that employee's `WeekOff`/`Holiday` attendance rows for the month
   (floored at 1), so a tracked weekly-off pattern is excluded from the payable-day
   denominator instead of every calendar day being treated as payable.
3. ~~**Coarse HR permission.**~~ **Fixed.** The single `PermissionModules.HumanResources`
   flag was split into `HrEmployees`, `HrAttendance`, `HrLeave`, `HrPayroll`, each
   independently gating its own WPF nav item. Existing Admin/Cashier role data was
   migrated via `20260716090000_SplitHumanResourcesPermission`; custom roles created
   through the Roles screen already pick up the four new modules automatically since
   role creation iterates `PermissionModules.All`.

Also fixed since the original version of this document:

4. ~~**No web UI for HR.**~~ **Fixed 2026-07-18.** `HotelPOS.Client` gained
   `EmployeesComponent`, `AttendanceComponent`, `LeaveComponent`, and `PayrollComponent`,
   routed under `/admin/*`, consuming the same REST API as the WPF app.
5. ~~**TDS is not computed.**~~ **Fixed.** `PayrollService.CalculatePayslip` now calls
   `TdsCalculator.CalculateMonthlyTds` against `TdsConfig`/`TdsSlab` (`TdsRuleSet`
   resolved per financial year via `IPayrollRepository.GetTdsRuleSetAsync`), landed in
   commit `fbe9f80` ("add TDS slab engine and Employee Self-Service portal"). New tax
   regime only — no old-regime/declared-exemption (80C/HRA) support, which is out of
   scope per `docs/PROJECT_ESTIMATION.md`. Covered by
   `PayrollServiceTests.CalculatePayslip_WithTdsRuleSet_ComputesNonZeroTds` and
   `TdsCalculatorTests`.
6. ~~**Action-level HR permissions still coarse.**~~ **Fixed.** The finer view-vs-run
   distinction already existed end-to-end at the domain/service/API layers — a separate
   `PermissionModules.HrPayrollRun` module (added by migration
   `20260728161025_AddHrPayrollRunPermission`) gates `SaveSalaryStructureAsync`,
   `RunPayrollAsync`, `MarkRunAsPaidAsync`, and `VoidRunAsync` via
   `IAuthorizationService.EnsureEditPermission`, separate from the read-only
   `PermissionModules.HrPayroll` access needed just to view the screen/runs/payslips —
   `PayrollController` itself carries only a bare `[Authorize]`, not
   `[Authorize(Roles = ...)]`, so this was never actually an API-attribute-level gate.
   The desktop UI just didn't reflect the distinction: `PayrollView`'s Save/Run/Mark-Paid
   buttons were always enabled for anyone who could see the screen at all, only failing
   with an error toast on click if they lacked `HrPayrollRun`. `PayrollViewModel` now
   takes `IAuthorizationService`, exposes `CanRunPayroll`
   (`HasEditPermission(PermissionModules.HrPayrollRun)`), and gates all three commands
   via `[RelayCommand(CanExecute = nameof(CanRunPayroll))]`, so WPF now disables (not
   just rejects) those buttons for a view-only HR user, with a tooltip explaining why.
   `IAuthorizationService.HasEditPermission` was promoted from the concrete
   `AuthorizationService` class onto the interface to make this possible from a
   DI-injected ViewModel. Covered by
   `PayrollViewModelTests.WithoutHrPayrollRunPermission_CanRunPayrollIsFalse_AndCommandsReportNotExecutable`
   and the paired `WithHrPayrollRunPermission_...` test.
   Angular's `PayrollComponent` has the identical UI gap (no `PermissionService` check
   gating its run/mark-paid buttons) — out of scope here since this pass targeted the
   desktop client specifically, but worth the same fix later for parity.

Still open:

7. **PII stored unencrypted.** PAN, Aadhaar, UAN, ESIC number, and bank account details
   are plain `nvarchar` columns with no column-level encryption or masking — worth a
   security review if this ever handles real employee data at scale.
8. **No employee self-service / notifications.** Applying for leave, viewing payslips,
   etc. all go through the same admin-facing WPF screens — there's no notification (e.g.
   email) when a leave request is approved/rejected, and no dedicated "my profile" view
   for a logged-in employee tied via `Employee.UserId`.
9. **No "Void Run" control in the WPF desktop UI.** `PayrollService.VoidRunAsync` /
   `PayrollController.VoidRun` exist and are already gated by `HrPayrollRun`, but
   `PayrollView.xaml` has no button wired to it.

## 8. Test Coverage

HR has substantial automated test coverage (~1,300 lines) across:

| File | Focus |
|---|---|
| `HotelPOS.Tests/Unit/Services/EmployeeServiceTests.cs` | Employee CRUD, code generation, validation |
| `HotelPOS.Tests/Unit/Services/AttendanceServiceTests.cs` | Mark/upsert, worked-hours calc |
| `HotelPOS.Tests/Unit/Services/LeaveServiceTests.cs` | Apply/approve/reject, balance initialization & sufficiency |
| `HotelPOS.Tests/Unit/Services/PayrollServiceTests.cs` | Payslip calculation, run lifecycle, statutory math |
| `HotelPOS.Tests/Unit/ViewModels/{Employee,Attendance,Leave,Payroll}*ViewModelTests.cs` | WPF ViewModel behavior |
| `HotelPOS.Tests/Unit/Controllers/HrControllersTests.cs` | All four HR controllers, role gating, error mapping |
| `HotelPOS.Tests/Integration/HrRepositoryTests.cs` | EF Core repository round-trips against the real `DbContext` |

This is comparable in depth to other core modules (Billing, Inventory), suggesting HR is
treated as production-grade rather than experimental.

## 9. Quick Reference — Service Interfaces

```
IEmployeeService     GetEmployeesAsync, GetEmployeeByIdAsync, SaveEmployeeAsync,
                      DeleteEmployeeAsync, ValidateEmployeeCodeUniqueAsync,
                      GetDepartmentsAsync, GetDesignationsAsync

IAttendanceService    GetAttendanceAsync, GetAttendanceForDateAsync,
                      MarkAttendanceAsync, DeleteAttendanceAsync

ILeaveService         GetLeaveTypesAsync, GetBalancesAsync, GetRequestsAsync,
                      ApplyLeaveAsync, ApproveLeaveAsync, RejectLeaveAsync

IPayrollService       GetSalaryStructuresAsync, SaveSalaryStructureAsync,
                      RunPayrollAsync, MarkRunAsPaidAsync, GetRunsAsync,
                      GetRunByIdAsync, GetPayslipsByEmployeeAsync
```
