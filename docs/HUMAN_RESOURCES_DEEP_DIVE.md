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
HotelPOS      -> WPF desktop views/viewmodels (the only client that surfaces HR today)
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
| TDS | **Not auto-computed** — hardcoded to 0, documented as a manual/statutory override |

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
| `PayrollController` | `/api/payroll` | Salary structure: Admin, Manager. Run/mark-paid: **Admin only**. |

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
- **The Angular web client (`HotelPOS.Client`) has no HR UI at all.** HR is currently a
  WPF-desktop-only capability; the REST API exists and is fully authorized, so a web
  front end could be added without touching the Application/Infrastructure layers.

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

Still open:

4. **No web UI for HR.** Only the WPF desktop app exposes Employee/Attendance/Leave/
   Payroll screens; the REST API is otherwise unused by `HotelPOS.Client`.
5. **PII stored unencrypted.** PAN, Aadhaar, UAN, ESIC number, and bank account details
   are plain `nvarchar` columns with no column-level encryption or masking — worth a
   security review if this ever handles real employee data at scale.
6. **TDS is not computed.** `PayrollService.CalculatePayslip` hardcodes TDS to 0;
   income-tax withholding must be entered/adjusted manually elsewhere (no mechanism to
   do so is visible in the current Payslip write path — it's effectively always 0 today).
7. **No employee self-service / notifications.** Applying for leave, viewing payslips,
   etc. all go through the same admin-facing WPF screens — there's no notification (e.g.
   email) when a leave request is approved/rejected, and no dedicated "my profile" view
   for a logged-in employee tied via `Employee.UserId`.
8. **Action-level HR permissions still coarse.** The new per-screen flags gate
   visibility, but finer distinctions (e.g. view payroll vs. run payroll) still rely
   solely on the API's `[Authorize(Roles = ...)]` checks, not the desktop permission
   model.

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
