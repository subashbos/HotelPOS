import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';

// Components
import { AdminNavbarComponent } from './components/navbars/admin-navbar/admin-navbar.component';
import { SidebarComponent } from './components/sidebar/sidebar.component';
import { HeaderStatsComponent } from './components/headers/header-stats/header-stats.component';
import { FooterAdminComponent } from './components/footers/footer-admin/footer-admin.component';
import { FooterSmallComponent } from './components/footers/footer-small/footer-small.component';
import { CardStatsComponent } from './components/cards/card-stats/card-stats.component';
import { CardLineChartComponent } from './components/cards/card-line-chart/card-line-chart.component';
import { CardBarChartComponent } from './components/cards/card-bar-chart/card-bar-chart.component';
import { CardSettingsComponent } from './components/cards/card-settings/card-settings.component';
import { CardTableComponent } from './components/cards/card-table/card-table.component';
import { TableDropdownComponent } from './components/dropdowns/table-dropdown/table-dropdown.component';
import { NotificationDropdownComponent } from './components/dropdowns/notification-dropdown/notification-dropdown.component';
import { UserDropdownComponent } from './components/dropdowns/user-dropdown/user-dropdown.component';

// Layouts
import { AdminComponent } from './layouts/admin/admin.component';
import { AuthComponent } from './layouts/auth/auth.component';

// Views
import { DashboardComponent } from './views/admin/dashboard/dashboard.component';
import { SettingsComponent } from './views/admin/settings/settings.component';
import { TablesComponent } from './views/admin/tables/tables.component';
import { LoginComponent } from './views/auth/login/login.component';
import { RegisterComponent } from './views/auth/register/register.component';
import { BillingComponent } from './views/admin/billing/billing.component';

@NgModule({
  declarations: [
    AdminNavbarComponent,
    SidebarComponent,
    HeaderStatsComponent,
    FooterAdminComponent,
    FooterSmallComponent,
    CardStatsComponent,
    CardLineChartComponent,
    CardBarChartComponent,
    CardSettingsComponent,
    CardTableComponent,
    TableDropdownComponent,
    NotificationDropdownComponent,
    UserDropdownComponent,
    AdminComponent,
    AuthComponent,
    DashboardComponent,
    SettingsComponent,
    TablesComponent,
    LoginComponent,
    RegisterComponent,
    BillingComponent
  ],
  imports: [
    CommonModule,
    RouterModule,
    FormsModule
  ],
  exports: [
    FormsModule,
    AdminNavbarComponent,
    SidebarComponent,
    HeaderStatsComponent,
    FooterAdminComponent,
    FooterSmallComponent,
    CardStatsComponent,
    CardLineChartComponent,
    CardBarChartComponent,
    CardSettingsComponent,
    CardTableComponent,
    TableDropdownComponent,
    NotificationDropdownComponent,
    UserDropdownComponent,
    AdminComponent,
    AuthComponent,
    DashboardComponent,
    SettingsComponent,
    TablesComponent,
    LoginComponent,
    RegisterComponent,
    BillingComponent
  ]
})
export class TemplateModule { }
