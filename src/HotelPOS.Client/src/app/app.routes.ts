import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';
import { permissionGuard } from './guards/permission.guard';

// layouts
import { AdminComponent } from "./layouts/admin/admin.component";
import { AuthComponent } from "./layouts/auth/auth.component";
import { EssComponent } from "./layouts/ess/ess.component";

// admin views
import { DashboardComponent } from "./views/admin/dashboard/dashboard.component";
import { SettingsComponent } from "./views/admin/settings/settings.component";
import { TablesComponent } from "./views/admin/tables/tables.component";
import { BillingComponent } from "./views/admin/billing/billing.component";
import { CategoriesComponent } from "./views/admin/categories/categories.component";
import { UnitsComponent } from "./views/admin/units/units.component";
import { ItemsComponent } from "./views/admin/items/items.component";
import { SuppliersComponent } from "./views/admin/suppliers/suppliers.component";
import { PurchasesComponent } from "./views/admin/purchases/purchases.component";
import { ShiftSessionComponent } from "./views/admin/shift-session/shift-session.component";
import { ExpensesComponent } from "./views/admin/expenses/expenses.component";
import { EmployeesComponent } from "./views/admin/employees/employees.component";
import { AttendanceComponent } from "./views/admin/attendance/attendance.component";
import { LeaveComponent } from "./views/admin/leave/leave.component";
import { PayrollComponent } from "./views/admin/payroll/payroll.component";
import { TdsComponent } from "./views/admin/tds/tds.component";
import { SalesReportComponent } from "./views/admin/sales-report/sales-report.component";
import { ItemReportComponent } from "./views/admin/item-report/item-report.component";
import { PurchaseReportComponent } from "./views/admin/purchase-report/purchase-report.component";
import { LedgerComponent } from "./views/admin/ledger/ledger.component";
import { JournalComponent } from "./views/admin/journal/journal.component";
import { RolesComponent } from "./views/admin/roles/roles.component";
import { UsersComponent } from "./views/admin/users/users.component";
import { AuditComponent } from "./views/admin/audit/audit.component";
import { AccountComponent } from "./views/admin/account/account.component";
import { CustomersComponent } from "./views/admin/customers/customers.component";
import { EstimationsComponent } from "./views/admin/estimations/estimations.component";
import { ReservationsComponent } from "./views/admin/reservations/reservations.component";
import { RawMaterialsComponent } from "./views/admin/raw-materials/raw-materials.component";
import { BomComponent } from "./views/admin/bom/bom.component";
import { BiAnalyticsComponent } from "./views/admin/bi-analytics/bi-analytics.component";
import { AccessDeniedComponent } from "./views/admin/access-denied/access-denied.component";

// auth views
import { LoginComponent } from "./views/auth/login/login.component";
import { RegisterComponent } from "./views/auth/register/register.component";
import { ForgotPasswordComponent } from "./views/auth/forgot-password/forgot-password.component";

// ess views
import { EssLeaveComponent } from "./views/ess/leave/ess-leave.component";
import { EssPayslipsComponent } from "./views/ess/payslips/ess-payslips.component";
import { EssProfileComponent } from "./views/ess/profile/ess-profile.component";

export const routes: Routes = [
  // admin views
  {
    path: "admin",
    component: AdminComponent,
    canActivate: [authGuard],
    canActivateChild: [authGuard],
    children: [
      { path: "dashboard", component: DashboardComponent, canActivate: [permissionGuard], data: { modules: ["Dashboard"] } },
      { path: "settings", component: SettingsComponent, canActivate: [permissionGuard], data: { modules: ["Settings"] } },
      { path: "tables", component: TablesComponent, canActivate: [permissionGuard], data: { modules: ["Tables"] } },
      { path: "billing", component: BillingComponent, canActivate: [permissionGuard], data: { modules: ["Billing"] } },
      { path: "categories", component: CategoriesComponent, canActivate: [permissionGuard], data: { modules: ["Categories"] } },
      { path: "units", component: UnitsComponent, canActivate: [permissionGuard], data: { modules: ["Units"] } },
      { path: "items", component: ItemsComponent, canActivate: [permissionGuard], data: { modules: ["Items"] } },
      { path: "suppliers", component: SuppliersComponent, canActivate: [permissionGuard], data: { modules: ["Purchase"] } },
      { path: "purchases", component: PurchasesComponent, canActivate: [permissionGuard], data: { modules: ["Purchase"] } },
      { path: "estimations", component: EstimationsComponent, canActivate: [permissionGuard], data: { modules: ["Estimation"] } },
      { path: "reservations", component: ReservationsComponent, canActivate: [permissionGuard], data: { modules: ["Reservation"] } },
      { path: "session", component: ShiftSessionComponent, canActivate: [permissionGuard], data: { modules: ["Shift"] } },
      { path: "expenses", component: ExpensesComponent, canActivate: [permissionGuard], data: { modules: ["Expenses"] } },
      { path: "employees", component: EmployeesComponent, canActivate: [permissionGuard], data: { modules: ["HrEmployees"] } },
      { path: "attendance", component: AttendanceComponent, canActivate: [permissionGuard], data: { modules: ["HrAttendance"] } },
      { path: "leave", component: LeaveComponent, canActivate: [permissionGuard], data: { modules: ["HrLeave"] } },
      { path: "payroll", component: PayrollComponent, canActivate: [permissionGuard], data: { modules: ["HrPayroll"] } },
      { path: "tds", component: TdsComponent, canActivate: [permissionGuard], data: { modules: ["Tds"] } },
      { path: "sales-report", component: SalesReportComponent, canActivate: [permissionGuard], data: { modules: ["SalesReport"] } },
      { path: "item-report", component: ItemReportComponent, canActivate: [permissionGuard], data: { modules: ["SalesReport"] } },
      { path: "purchase-report", component: PurchaseReportComponent, canActivate: [permissionGuard], data: { modules: ["Purchase", "SalesReport"] } },
      { path: "ledger", component: LedgerComponent, canActivate: [permissionGuard], data: { modules: ["Ledger"] } },
      { path: "journal", component: JournalComponent, canActivate: [permissionGuard], data: { modules: ["Journal"] } },
      { path: "roles", component: RolesComponent, canActivate: [permissionGuard], data: { modules: ["Roles"] } },
      { path: "users", component: UsersComponent, canActivate: [permissionGuard], data: { modules: ["Settings"] } },
      { path: "audit", component: AuditComponent, canActivate: [permissionGuard], data: { modules: ["Audit"] } },
      { path: "account", component: AccountComponent },
      { path: "customers", component: CustomersComponent, canActivate: [permissionGuard], data: { modules: ["Customers"] } },
      { path: "raw-materials", component: RawMaterialsComponent, canActivate: [permissionGuard], data: { modules: ["Purchase"] } },
      { path: "bom", component: BomComponent, canActivate: [permissionGuard], data: { modules: ["Purchase"] } },
      { path: "bi-analytics", component: BiAnalyticsComponent, canActivate: [permissionGuard], data: { modules: ["SalesReport"] } },
      { path: "forbidden", component: AccessDeniedComponent },
      { path: "", redirectTo: "dashboard", pathMatch: "full" },
    ],
  },
  // employee self-service views
  {
    path: "ess",
    component: EssComponent,
    canActivate: [authGuard],
    canActivateChild: [authGuard],
    children: [
      { path: "leave", component: EssLeaveComponent },
      { path: "payslips", component: EssPayslipsComponent },
      { path: "profile", component: EssProfileComponent },
      { path: "", redirectTo: "leave", pathMatch: "full" },
    ],
  },
  // auth views at root
  {
    path: "",
    component: AuthComponent,
    children: [
      { path: "", component: LoginComponent },
      { path: "login", component: LoginComponent },
      { path: "register", component: RegisterComponent },
      { path: "forgot-password", component: ForgotPasswordComponent },
    ],
  },
  { path: "**", redirectTo: "", pathMatch: "full" },
];
