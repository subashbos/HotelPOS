import { Component, OnInit } from "@angular/core";
import { SidebarComponent } from "../../components/sidebar/sidebar.component";
import { AdminNavbarComponent } from "../../components/navbars/admin-navbar/admin-navbar.component";
import { HeaderStatsComponent } from "../../components/headers/header-stats/header-stats.component";
import { FooterAdminComponent } from "../../components/footers/footer-admin/footer-admin.component";
import { RouterOutlet } from "@angular/router";

@Component({ standalone: false,   
  selector: "app-admin",
  
  
  templateUrl: "./admin.component.html",
})
export class AdminComponent implements OnInit {
  constructor() {}

  ngOnInit(): void {}
}
