import { Component, OnInit } from "@angular/core";
import { PermissionService } from "../../services/permission.service";

@Component({
  standalone: false,
  selector: "app-sidebar",
  templateUrl: "./sidebar.component.html",
})
export class SidebarComponent implements OnInit {
  collapseShow = "hidden";

  constructor(private readonly permissionService: PermissionService) {}

  ngOnInit(): void {
    this.permissionService.ensureLoaded().subscribe();
  }

  toggleCollapseShow(classes: string) {
    this.collapseShow = classes;
  }

  /** Nav links stay hidden until the permission snapshot loads, then reflect it exactly. */
  has(module: string): boolean {
    return this.permissionService.hasPermission(module);
  }

  hasAny(modules: string[]): boolean {
    return modules.some(module => this.has(module));
  }
}
