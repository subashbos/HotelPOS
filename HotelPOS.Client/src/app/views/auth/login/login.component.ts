import { Component, OnInit } from "@angular/core";
import { Router } from "@angular/router";
import { AuthService } from "../../../services/auth.service";

@Component({
  
  selector: "app-login",
  templateUrl: "./login.component.html",
})
export class LoginComponent implements OnInit {
  credentials = {
    username: "",
    password: ""
  };
  errorMessage = "";
  isLoading = false;

  constructor(private authService: AuthService, private router: Router) {}

  ngOnInit(): void {
    // If already logged in, redirect to admin dashboard
    if (this.authService.isLoggedIn()) {
      this.router.navigate(["/admin/dashboard"]);
    }
  }

  onSubmit(): void {
    if (!this.credentials.username || !this.credentials.password) {
      this.errorMessage = "Please enter both username and password.";
      return;
    }

    this.isLoading = true;
    this.errorMessage = "";

    this.authService.login(this.credentials).subscribe({
      next: () => {
        this.isLoading = false;
        // Redirect to admin dashboard
        this.router.navigate(["/admin/dashboard"]);
      },
      error: (err) => {
        this.isLoading = false;
        if (err.error?.message) {
          this.errorMessage = err.error.message;
        } else if (err.status === 401) {
          this.errorMessage = "Invalid username or password.";
        } else {
          this.errorMessage = "An unexpected error occurred. Please try again.";
        }
      }
    });
  }
}
