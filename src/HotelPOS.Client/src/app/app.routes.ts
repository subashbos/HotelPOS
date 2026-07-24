import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';

// layouts
import { AdminComponent } from "./layouts/admin/admin.component";
import { AuthComponent } from "./layouts/auth/auth.component";

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
import { RawMaterialsComponent } from "./views/admin/raw-materials/raw-materials.component";
import { BomComponent } from "./views/admin/bom/bom.component";
import { BiAnalyticsComponent } from "./views/admin/bi-analytics/bi-analytics.component";

// auth views
import { LoginComponent } from "./views/auth/login/login.component";
import { RegisterComponent } from "./views/auth/register/register.component";
import { ForgotPasswordComponent } from "./views/auth/forgot-password/forgot-password.component";

export const routes: Routes = [
  // admin views
  {
    path: "admin",
    component: AdminComponent,
    canActivate: [authGuard],
    canActivateChild: [authGuard],
    children: [
      { path: "dashboard", component: DashboardComponent },
      { path: "settings", component: SettingsComponent },
      { path: "tables", component: TablesComponent },
      { path: "billing", component: BillingComponent },
      { path: "categories", component: CategoriesComponent },
      { path: "units", component: UnitsComponent },
      { path: "items", component: ItemsComponent },
      { path: "suppliers", component: SuppliersComponent },
      { path: "purchases", component: PurchasesComponent },
      { path: "session", component: ShiftSessionComponent },
      { path: "expenses", component: ExpensesComponent },
      { path: "employees", component: EmployeesComponent },
      { path: "attendance", component: AttendanceComponent },
      { path: "leave", component: LeaveComponent },
      { path: "payroll", component: PayrollComponent },
      { path: "sales-report", component: SalesReportComponent },
      { path: "item-report", component: ItemReportComponent },
      { path: "purchase-report", component: PurchaseReportComponent },
      { path: "ledger", component: LedgerComponent },
      { path: "journal", component: JournalComponent },
      { path: "roles", component: RolesComponent },
      { path: "users", component: UsersComponent },
      { path: "audit", component: AuditComponent },
      { path: "account", component: AccountComponent },
      { path: "customers", component: CustomersComponent },
      { path: "raw-materials", component: RawMaterialsComponent },
      { path: "bom", component: BomComponent },
      { path: "bi-analytics", component: BiAnalyticsComponent },
      { path: "", redirectTo: "dashboard", pathMatch: "full" },
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
