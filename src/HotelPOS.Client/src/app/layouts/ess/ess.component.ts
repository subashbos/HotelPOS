import { Component } from "@angular/core";
import { Router } from "@angular/router";
import { AuthService } from "../../services/auth.service";

@Component({
  standalone: false,
  selector: "app-ess",
  templateUrl: "./ess.component.html",
})
export class EssComponent {
  constructor(private readonly authService: AuthService, private readonly router: Router) {}

  get username(): string | null {
    return this.authService.getUsername();
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(["/login"]);
  }
}
