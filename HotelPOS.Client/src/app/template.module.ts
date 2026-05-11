import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

// Components
import { IndexNavbarComponent } from './components/navbars/index-navbar/index-navbar.component';
import { AuthNavbarComponent } from './components/navbars/auth-navbar/auth-navbar.component';
import { AdminNavbarComponent } from './components/navbars/admin-navbar/admin-navbar.component';
import { SidebarComponent } from './components/sidebar/sidebar.component';
import { HeaderStatsComponent } from './components/headers/header-stats/header-stats.component';
import { FooterComponent } from './components/footers/footer/footer.component';
import { FooterAdminComponent } from './components/footers/footer-admin/footer-admin.component';
import { FooterSmallComponent } from './components/footers/footer-small/footer-small.component';
import { CardStatsComponent } from './components/cards/card-stats/card-stats.component';
import { CardLineChartComponent } from './components/cards/card-line-chart/card-line-chart.component';
import { CardBarChartComponent } from './components/cards/card-bar-chart/card-bar-chart.component';
import { CardPageVisitsComponent } from './components/cards/card-page-visits/card-page-visits.component';
import { CardSocialTrafficComponent } from './components/cards/card-social-traffic/card-social-traffic.component';
import { CardSettingsComponent } from './components/cards/card-settings/card-settings.component';
import { CardProfileComponent } from './components/cards/card-profile/card-profile.component';
import { CardTableComponent } from './components/cards/card-table/card-table.component';
import { TableDropdownComponent } from './components/dropdowns/table-dropdown/table-dropdown.component';
import { PagesDropdownComponent } from './components/dropdowns/pages-dropdown/pages-dropdown.component';
import { NotificationDropdownComponent } from './components/dropdowns/notification-dropdown/notification-dropdown.component';
import { UserDropdownComponent } from './components/dropdowns/user-dropdown/user-dropdown.component';
import { IndexDropdownComponent } from './components/dropdowns/index-dropdown/index-dropdown.component';
import { MapExampleComponent } from './components/maps/map-example/map-example.component';

// Layouts
import { AdminComponent } from './layouts/admin/admin.component';
import { AuthComponent } from './layouts/auth/auth.component';

// Views
import { DashboardComponent } from './views/admin/dashboard/dashboard.component';
import { MapsComponent } from './views/admin/maps/maps.component';
import { SettingsComponent } from './views/admin/settings/settings.component';
import { TablesComponent } from './views/admin/tables/tables.component';
import { LoginComponent } from './views/auth/login/login.component';
import { RegisterComponent } from './views/auth/register/register.component';
import { IndexComponent } from './views/index/index.component';
import { LandingComponent } from './views/landing/landing.component';
import { ProfileComponent } from './views/profile/profile.component';

@NgModule({
  declarations: [
    IndexNavbarComponent,
    AuthNavbarComponent,
    AdminNavbarComponent,
    SidebarComponent,
    HeaderStatsComponent,
    FooterComponent,
    FooterAdminComponent,
    FooterSmallComponent,
    CardStatsComponent,
    CardLineChartComponent,
    CardBarChartComponent,
    CardPageVisitsComponent,
    CardSocialTrafficComponent,
    CardSettingsComponent,
    CardProfileComponent,
    CardTableComponent,
    TableDropdownComponent,
    PagesDropdownComponent,
    NotificationDropdownComponent,
    UserDropdownComponent,
    IndexDropdownComponent,
    MapExampleComponent,
    AdminComponent,
    AuthComponent,
    DashboardComponent,
    MapsComponent,
    SettingsComponent,
    TablesComponent,
    LoginComponent,
    RegisterComponent,
    IndexComponent,
    LandingComponent,
    ProfileComponent
  ],
  imports: [
    CommonModule,
    RouterModule
  ],
  exports: [
    IndexNavbarComponent,
    AuthNavbarComponent,
    AdminNavbarComponent,
    SidebarComponent,
    HeaderStatsComponent,
    FooterComponent,
    FooterAdminComponent,
    FooterSmallComponent,
    CardStatsComponent,
    CardLineChartComponent,
    CardBarChartComponent,
    CardPageVisitsComponent,
    CardSocialTrafficComponent,
    CardSettingsComponent,
    CardProfileComponent,
    CardTableComponent,
    TableDropdownComponent,
    PagesDropdownComponent,
    NotificationDropdownComponent,
    UserDropdownComponent,
    IndexDropdownComponent,
    MapExampleComponent,
    AdminComponent,
    AuthComponent,
    DashboardComponent,
    MapsComponent,
    SettingsComponent,
    TablesComponent,
    LoginComponent,
    RegisterComponent,
    IndexComponent,
    LandingComponent,
    ProfileComponent
  ]
})
export class TemplateModule { }
