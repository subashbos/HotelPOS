import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../services/auth.service';

@Component({
  standalone: false,
  selector: 'app-forgot-password',
  templateUrl: './forgot-password.component.html',
})
export class ForgotPasswordComponent {
  step: 'request' | 'confirm' = 'request';

  username = '';
  code = '';
  newPassword = '';
  confirmPassword = '';

  isLoading = false;
  errorMessage = '';
  infoMessage = '';

  constructor(private readonly authService: AuthService, private readonly router: Router) {}

  requestCode(): void {
    if (!this.username.trim() || this.isLoading) return;
    this.isLoading = true;
    this.errorMessage = '';
    this.authService.forgotPassword(this.username.trim()).subscribe({
      next: () => {
        this.isLoading = false;
        this.step = 'confirm';
        this.infoMessage = 'If an account with that username has an email on file, a reset code has been sent to it.';
      },
      error: () => {
        this.isLoading = false;
        // Never reveal whether the account exists — treat as sent either way.
        this.step = 'confirm';
        this.infoMessage = 'If an account with that username has an email on file, a reset code has been sent to it.';
      }
    });
  }

  confirmReset(): void {
    if (!this.code.trim() || !this.newPassword || this.isLoading) return;
    if (this.newPassword !== this.confirmPassword) {
      this.errorMessage = 'Passwords do not match.';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.authService.resetPasswordWithCode(this.username.trim(), this.code.trim(), this.newPassword).subscribe({
      next: () => {
        this.isLoading = false;
        this.router.navigate(['/login']);
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.message || err.error?.Message || 'This code is invalid or has expired.';
      }
    });
  }

  backToRequest(): void {
    this.step = 'request';
    this.errorMessage = '';
    this.infoMessage = '';
  }
}
