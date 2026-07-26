import { Component, OnInit } from "@angular/core";
import { Router } from "@angular/router";
import { AuthService } from "../../../services/auth.service";

// Roles that use the full admin console; every other role (e.g. a lower-trust "Employee" role
// with no HR/admin permissions) lands in the Employee Self-Service portal instead.
const ADMIN_CONSOLE_ROLES = ["Admin", "Manager", "Cashier"];

@Component({
  standalone: false,
  
  selector: "app-login",
  templateUrl: "./login.component.html",
})
export class LoginComponent implements OnInit {
  credentials = {
    username: "",
    password: ""
  };
  totpCode = "";
  requiresTwoFactor = false;
  errorMessage = "";
  isLoading = false;

  constructor(private readonly authService: AuthService, private readonly router: Router) {}

  ngOnInit(): void {
    // If already logged in, redirect to the appropriate area for the current role
    if (this.authService.isLoggedIn()) {
      this.router.navigate([this.landingRouteForRole()]);
    }
  }

  onSubmit(): void {
    if (!this.credentials.username || !this.credentials.password) {
      this.errorMessage = "Please enter both username and password.";
      return;
    }

    if (this.requiresTwoFactor && !this.totpCode) {
      this.errorMessage = "Enter your 6-digit authentication code.";
      return;
    }

    this.isLoading = true;
    this.errorMessage = "";

    const payload = this.requiresTwoFactor
      ? { ...this.credentials, totpCode: this.totpCode }
      : this.credentials;

    this.authService.login(payload).subscribe({
      next: () => {
        this.isLoading = false;
        this.router.navigate([this.landingRouteForRole()]);
      },
      error: (err) => {
        this.isLoading = false;
        if (err.status === 401 && err.error?.requiresTwoFactor) {
          this.requiresTwoFactor = true;
          this.totpCode = "";
          this.errorMessage = "Enter the 6-digit code from your authenticator app.";
        } else if (err.error?.message) {
          this.errorMessage = err.error.message;
        } else if (err.status === 401) {
          this.errorMessage = "Invalid username or password.";
        } else {
          this.errorMessage = "An unexpected error occurred. Please try again.";
        }
      }
    });
  }

  private landingRouteForRole(): string {
    const role = this.authService.getRole();
    return role && ADMIN_CONSOLE_ROLES.includes(role) ? "/admin/dashboard" : "/ess/leave";
  }

  useDifferentAccount(): void {
    this.requiresTwoFactor = false;
    this.totpCode = "";
    this.credentials.password = "";
    this.errorMessage = "";
  }
}
