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
import { ItemsComponent } from "./views/admin/items/items.component";
import { SuppliersComponent } from "./views/admin/suppliers/suppliers.component";
import { PurchasesComponent } from "./views/admin/purchases/purchases.component";
import { ShiftSessionComponent } from "./views/admin/shift-session/shift-session.component";
import { ExpensesComponent } from "./views/admin/expenses/expenses.component";

// auth views
import { LoginComponent } from "./views/auth/login/login.component";
import { RegisterComponent } from "./views/auth/register/register.component";

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
      { path: "items", component: ItemsComponent },
      { path: "suppliers", component: SuppliersComponent },
      { path: "purchases", component: PurchasesComponent },
      { path: "session", component: ShiftSessionComponent },
      { path: "expenses", component: ExpensesComponent },
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
    ],
  },
  { path: "**", redirectTo: "", pathMatch: "full" },
];
