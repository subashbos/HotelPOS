# HotelPOS — Remaining Work Estimation (Process & Man-Hours)

This document estimates the effort to close the **open items already identified
in the codebase's own documentation** — it does not invent new scope. Every
line item below traces back to a specific "Open" / "In Progress" / "still
open" entry in `QA_REVIEW_AND_TEST_GAPS.md`, the risk register in
`KNOWLEDGE_TRANSFER.md` §8, or the gaps section in `HUMAN_RESOURCES_DEEP_DIVE.md`
§7. See "Source" column for the exact reference.

---

## 1. Process

1. **Backlog sourced from existing self-review docs**, not re-discovered —
   the project already runs continuous QA (per-PR CI gating, dated risk
   registers), so the gaps are pre-identified with root-cause context already
   written down. This cuts discovery/triage time out of the estimate.
2. **Triage by risk class**: security/compliance items first (data exposure,
   auth-adjacent), then reliability/architecture (service lifetimes, backup
   verification), then access-control granularity, then feature completeness
   (ESS notifications), then remaining test-coverage gaps, then documentation.
3. **Fix + test + doc-update per item** — each item's estimate includes
   writing the fix, the regression test proving it, and updating the source
   doc's status line from `Open`/`still open` to `Fixed`, consistent with how
   every prior fix in this repo is recorded (see the "FIXED" pattern already
   used throughout `QA_REVIEW_AND_TEST_GAPS.md`).
4. **CI-gated delivery** — each item lands as its own PR through the existing
   `dotnet.yml`/`eslint.yml`/`codeql.yml`/SonarCloud gates, same as all prior
   work in this repo.

---

## 2. Backlog & Man-Hours

### 2.1 Security & Compliance Hardening

| Item | Source | Hours |
|---|---|---:|
| Column-level encryption/masking for PII (PAN, Aadhaar, UAN, ESIC number, bank details) | `HUMAN_RESOURCES_DEEP_DIVE.md` §7 item 5 | 70 |
| Wire TDS auto-computation into `PayrollService.CalculatePayslip` (currently hardcoded to 0) using existing `TdsConfig`/`TdsSlab` entities | `HUMAN_RESOURCES_DEEP_DIVE.md` §7 item 6 | 45 |
| Investigate & fix systemic gap: void `IRequest` commands with FluentValidation validators not reliably passing through `ValidationBehavior` (only Purchases has a narrow patch) | `QA_REVIEW_AND_TEST_GAPS.md` item 8 | 30 |
| Deployment checklist step + tooling to rotate/disable the bootstrap admin account post go-live | `KNOWLEDGE_TRANSFER.md` Risk 5, remaining action | 8 |
| **Subtotal** | | **153** |

### 2.2 Reliability & Architecture

| Item | Source | Hours |
|---|---|---:|
| Audit all singleton services/constructor graphs; convert to scoped/transient where request/session state leaks in; add multi-scope create/dispose integration test | `KNOWLEDGE_TRANSFER.md` Risk 3 (Open, P1) | 40 |
| Periodic restore-drill process + automated `.bak`/`.db` integrity verification | `KNOWLEDGE_TRANSFER.md` Risk 1, remaining action | 20 |
| **Subtotal** | | **60** |

### 2.3 Access Control Granularity

| Item | Source | Hours |
|---|---|---:|
| Action-level HR permissions (e.g. view payroll vs. run payroll) surfaced in the desktop permission model, not just API `[Authorize(Roles=...)]` | `HUMAN_RESOURCES_DEEP_DIVE.md` §7 item 8 | 30 |
| **Subtotal** | | **30** |

### 2.4 Employee Self-Service Completeness

| Item | Source | Hours |
|---|---|---:|
| Notifications (e.g. email) on leave request approve/reject | `HUMAN_RESOURCES_DEEP_DIVE.md` §7 item 7 | 30 |
| Dedicated "my profile" self-service view tied to `Employee.UserId` | `HUMAN_RESOURCES_DEEP_DIVE.md` §7 item 7 | 25 |
| **Subtotal** | | **55** |

### 2.5 Test Coverage Closure

| Item | Source | Hours |
|---|---|---:|
| `HotelDbContext_HasUniqueInvoiceIndex_PerFiscalYear` test | `QA_REVIEW_AND_TEST_GAPS.md`, still open | 4 |
| `OrderService_SaveOrderAsync_ZeroOrNegativeQuantity_Throws` test | `QA_REVIEW_AND_TEST_GAPS.md`, still open | 3 |
| `OrderService_UpdateOrderAsync_WhenNewStockDeductionFails_RollsBackOldStockReturn` test | `QA_REVIEW_AND_TEST_GAPS.md`, still open | 6 |
| `UserService_ResetPasswordAsync` below-minimum-length (non-empty) password case | `QA_REVIEW_AND_TEST_GAPS.md`, still open | 3 |
| Fiscal-year invoice transition/rollover sequence tests | `KNOWLEDGE_TRANSFER.md` Risk 4, action 1 | 10 |
| Backup behavior tests validated per DB provider | `KNOWLEDGE_TRANSFER.md` Risk 4, action 2 | 10 |
| End-to-end soft-delete filtering verification across repositories | `KNOWLEDGE_TRANSFER.md` Risk 4, action 3 | 12 |
| Continue ratcheting API/backend coverage threshold toward 80% (currently 75%) | `QA_REVIEW_AND_TEST_GAPS.md` CI Coverage Gate section | 20 |
| **Subtotal** | | **68** |

### 2.6 Documentation

| Item | Source | Hours |
|---|---|---:|
| Data integrity doc section: soft-delete behavior for orders, hard- vs soft-delete policy for items/categories referenced by historical bills, negative-stock policy | `QA_REVIEW_AND_TEST_GAPS.md`, "Documentation Improvements" item 4 | 8 |
| **Subtotal** | | **8** |

---

## 3. Total Effort

| Category | Hours |
|---|---:|
| Security & compliance hardening | 153 |
| Reliability & architecture | 60 |
| Access control granularity | 30 |
| Employee self-service completeness | 55 |
| Test coverage closure | 68 |
| Documentation | 8 |
| Subtotal (delivery) | 374 |
| PM/coordination & PR-review overlay (12%) | 45 |
| **Grand total** | **≈ 419 hours** |

---

## 4. Timeline by Team Size

| Team composition | Effective capacity/week | Estimated duration |
|---|---:|---:|
| 1 full-stack developer (solo) | 40 hrs | ~10.5 weeks (≈ 2.5 months) |
| 2 developers (backend + full-stack) sharing QA | ~72 hrs (~90% efficiency) | ~6 weeks |
| 3 people (2 dev, 1 QA) | ~100 hrs (~85% efficiency) | ~4 weeks |

A 2-person team is the recommended baseline — most items are independent
(security, ESS, tests, docs can proceed in parallel with minimal shared-file
contention), so two developers get near-linear speedup without the
coordination tax a larger team would add for a backlog this size.

---

## 5. Suggested Priority Order

1. **Security & compliance** (§2.1) — PII exposure and the validation-pipeline
   gap are the highest-risk items; TDS being hardcoded to 0 is a compliance
   gap for any real payroll run.
2. **Reliability & architecture** (§2.2) — singleton/scoped audit reduces risk
   of hard-to-debug production incidents as usage grows.
3. **Test coverage closure** (§2.5) — cheap, well-defined, de-risks the above
   two categories as they land.
4. **Access control granularity** (§2.3) and **ESS completeness** (§2.4) —
   product-completeness items, lower risk, can slot in parallel with the
   above once security work is underway.
5. **Documentation** (§2.6) — closes out the last open item in
   `QA_REVIEW_AND_TEST_GAPS.md`, low effort.

## 6. Assumptions & Exclusions

- Assumes the fixes follow the same patterns already established in this
  codebase (e.g. FluentValidation for new rules, HTTP-level integration tests
  per the `CustomWebApplicationFactory` pattern) — no new architectural
  approach needs to be introduced.
- PII encryption approach (column-level EF Core value converters vs.
  database-native encryption) is not yet decided; 70 hours assumes an EF Core
  value-converter approach consistent with the existing stack. A
  database-native/HSM-backed approach would cost more.
- Excludes any new feature requests not already tracked as an open item in
  the repo's own documentation.
- A **15% contingency** (≈ 63 hours) on top of the grand total is recommended,
  consistent with the main `PROJECT_ESTIMATION.md`.
