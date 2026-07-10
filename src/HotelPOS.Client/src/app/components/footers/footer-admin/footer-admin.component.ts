import { Component } from "@angular/core";

@Component({
  standalone: false,
  selector: "app-footer-admin",
  templateUrl: "./footer-admin.component.html",
})
export class FooterAdminComponent {
  date = new Date().getFullYear();
  constructor() {}
}
