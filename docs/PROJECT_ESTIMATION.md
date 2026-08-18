# HotelPOS — Project Estimation (Process & Man-Hours)

This document estimates the effort to build HotelPOS **from scratch to its current
state**: a Clean Architecture solution with a WPF desktop client, an Angular web
client, a JWT-secured REST API, Indian GST/payroll compliance, and a 1,196-test
automated suite. It is meant as a reference for planning follow-on work, staffing,
or quoting comparable modules — not as a record of hours actually spent.

**Basis of estimate** (measured directly from the codebase):

| Metric | Count |
|---|---|
| Domain entities | 37 |
| API controllers | 22 |
| EF Core migrations | 52 |
| WPF views (XAML) | 59 |
| Angular components | 55 |
| Angular routes | 43 |
| Automated tests | 1,196 (423 core/WPF + 773 API) across 185 test files |
| Lines of code | ~135,000 C# (incl. tests) + ~15,600 TypeScript |
| CI pipelines | `dotnet.yml`, `eslint.yml`, `codeql.yml` + SonarCloud coverage gating |

---

## 1. Development Process

The codebase's layering, migration history, and CI setup are consistent with an
**iterative, module-by-module Agile process** built bottom-up through Clean
Architecture layers, with continuous QA rather than a single end-of-project test
phase:

1. **Discovery & Architecture** — define the Clean Architecture layering
   (Domain → Application → Infrastructure → API → Presentation), pick the stack
   (.NET 10, EF Core, MediatR/CQRS, WPF + Angular dual clients), and set up the
   solution, DI, and base repository/CQRS scaffolding.
2. **Foundation & Security** — auth (JWT + refresh tokens), roles/permissions,
   audit logging, password reset/lockout — built first since every later module
   depends on it.
3. **Per-module iterative delivery** — for each business module: Domain entity →
   EF Core migration → Application UseCase (Command/Query + FluentValidation) →
   API controller → WPF view/ViewModel → Angular component/route → unit +
   integration tests. Modules were clearly layered incrementally (52 migrations,
   each scoped to one feature slice, e.g. `AddHumanResourcesModule`,
   `AddLeaveBalancePendingDays`, `SplitHumanResourcesPermission`).
4. **Continuous integration & QA gating** — every change runs through
   `dotnet.yml` (build + test), `eslint.yml` (Angular lint), `codeql.yml`
   (security scanning), and SonarCloud coverage thresholds (ratcheted up over
   time, e.g. to 75% for API/backend) — QA is enforced per-PR, not batched at
   the end.
5. **Hardening & gap closure** — dedicated passes to close identified gaps
   (`QA_REVIEW_AND_TEST_GAPS.md`), fix concurrency/race issues (leave balance
   reservation, payroll proration), and raise security posture (PBKDF2 iteration
   count).
6. **Documentation & release readiness** — architecture docs, HR deep-dive,
   knowledge transfer doc, operations runbook, and a release QA checklist.

---

## 2. Man-Hour Estimate by Work Area

Each module figure is **full-stack**: Domain entity + migration, Application
UseCase/Service + validation, Infrastructure repository, API controller, WPF
view/ViewModel, Angular component (where present), and its unit/integration
tests — mirroring how the repo is actually structured.

### 2.1 Foundation & Cross-Cutting (built once, everything else depends on it)

| Work item | Hours |
|---|---:|
| Requirements analysis & architecture design (layering, stack selection) | 60 |
| Solution scaffolding, DI, EF Core base, generic repository, MediatR/CQRS pipeline | 70 |
| Auth & security infra (JWT, refresh tokens, `RolePermission`, `LoginLockout`, `PasswordResetRequest`, `RememberMeToken`, `AuditLog`) | 90 |
| WPF app shell (dashboard shell, navigation, theming, keyboard-shortcut framework, scoped-`DbContext` thread-safety infra) | 70 |
| Angular app shell (routing, auth guards/interceptors, shared services) | 60 |
| Backup & disaster recovery (replication, one-click SQL Server/SQLite restore) | 40 |
| CI/CD pipelines (dotnet, eslint, CodeQL, SonarCloud coverage gates) | 30 |
| **Subtotal** | **420** |

### 2.2 Core Business Modules

| Module | Hours |
|---|---:|
| Catalog management (Item, Category, UnitOfMeasurement, RawMaterial, BOM) | 110 |
| Billing & cart engine (`CartService`, Order/OrderItem, split/partial payments, refunds/voids, table transfer, KOT hold) | 180 |
| Table & kitchen management (layout, live status, transfers) | 60 |
| Print & receipt engine (thermal 80mm + A4, Tax Invoice/Bill of Supply, KOT tickets) | 70 |
| GST tax & compliance engine (Regular vs Composition scheme, CGST/SGST, round-off, TDS config/slabs) | 90 |
| Purchases & suppliers (Purchase, PurchaseItem, Supplier) | 90 |
| Expenses | 40 |
| Customers | 40 |
| Cash sessions (shift open/close, reconciliation) | 60 |
| Reports & BI (profit margins, food-cost trend, predictive low-stock alerts, Excel export via ClosedXML) | 110 |
| Wastage tracking (spoilage/damage/overproduction, cost fallback logic) | 40 |
| Settings (hotel profile, GST scheme toggle, printer config) | 40 |
| Users, roles & permissions UI (on top of the auth infra above) | 60 |
| Audit log viewer | 30 |
| **Subtotal** | **1,020** |

### 2.3 Human Resources Module

| Module | Hours |
|---|---:|
| Employee master (Employee, Department, Designation, reporting-manager hierarchy) | 80 |
| Attendance (upsert marking, worked-hours calculation) | 60 |
| Leave management (types, balances with pending-day reservation, request workflow) | 90 |
| Payroll (salary structure, run lifecycle, PF/ESI/Professional Tax statutory calc, LOP/proration) | 140 |
| Employee self-service (ESS) endpoints | 30 |
| **Subtotal** | **400** |

### 2.4 Testing, QA & Hardening (beyond tests embedded per-module above)

| Work item | Hours |
|---|---:|
| Integration/HTTP-level test harness & fixtures (773 API + 423 core/WPF tests, seed data) | 220 |
| Security hardening (PBKDF2 600k iterations, JWT key management, CORS lockdown) | 30 |
| Dedicated QA gap review & closure pass | 60 |
| Coverage gating & ratcheting in CI | 20 |
| **Subtotal** | **330** |

### 2.5 Documentation & Release

| Work item | Hours |
|---|---:|
| Architecture & technical docs (System Design, Technical Reference, HR deep-dive) | 35 |
| Knowledge transfer & operations runbook | 20 |
| Release QA checklist & release process | 15 |
| **Subtotal** | **70** |

### 2.6 Project Management & Coordination

Estimated at **12%** of all delivery effort above (2.1–2.5 = 2,240 hrs) for
planning, sprint coordination, PR review, and stakeholder communication:

| Work item | Hours |
|---|---:|
| Project management / coordination overlay (12%) | 270 |

---

## 3. Total Effort

| Category | Hours |
|---|---:|
| Foundation & cross-cutting | 420 |
| Core business modules | 1,020 |
| Human Resources module | 400 |
| Testing, QA & hardening | 330 |
| Documentation & release | 70 |
| Project management (12% overlay) | 270 |
| **Grand total** | **≈ 2,510 hours** |

---

## 4. Timeline by Team Size

Assuming 40 productive hours/week per person and accounting for coordination
overhead as team size grows (parallelization is never 100% efficient — module
dependencies and code review add friction):

| Team composition | Effective capacity/week | Estimated duration |
|---|---:|---:|
| 1 full-stack developer (solo) | 40 hrs | ~63 weeks (≈ 14–15 months) |
| 2 developers (1 backend/API, 1 WPF+Angular) + shared QA | ~72 hrs (~90% efficiency) | ~35 weeks (≈ 8 months) |
| 4 people (2 full-stack, 1 frontend/Angular, 1 QA) | ~128 hrs (~80% efficiency) | ~20 weeks (≈ 4.5–5 months) |
| 6 people (3 full-stack, 1 Angular, 1 QA, 1 part-time PM) | ~168 hrs (~70% efficiency) | ~15 weeks (≈ 3.5 months) |

The 4-person team is the recommended baseline: it parallelizes backend/API,
WPF, and Angular work along the same seams the codebase already reflects,
without the review-and-merge overhead a 6-person team incurs on a
single-solution-file codebase.

---

## 5. Assumptions & Exclusions

- Estimates assume an experienced team already fluent in .NET/EF Core/WPF and
  Angular — ramp-up time for an unfamiliar stack is **not** included.
- Excludes ongoing production support, hosting/infrastructure setup, and
  end-user training.
- Excludes UI/UX visual design (wireframing, branding) — assumes design
  direction is provided or kept minimal/utilitarian as in the current app.
- Indian GST/PF/ESI statutory rules are assumed stable during development;
  regulatory changes requiring rework are out of scope.
- TDS auto-computation is out of scope, matching the current codebase (§7 of
  `HUMAN_RESOURCES_DEEP_DIVE.md` notes it's hardcoded to 0 today).
- Figures are planning estimates, not a fixed-bid quote — actual effort varies
  with team seniority, requirement churn, and integration surprises. A
  **15–20% contingency buffer** on top of the grand total (≈ 375–500 hours) is
  recommended for any real schedule commitment.
