import { Component } from '@angular/core';
import { AuthService } from '../../../services/auth.service';
import { UserService } from '../../../services/user.service';

@Component({
  standalone: false,
  selector: 'app-account',
  templateUrl: './account.component.html',
})
export class AccountComponent {
  readonly username: string | null;

  // ── Change password ──
  newPassword = '';
  confirmPassword = '';
  isSavingPassword = false;
  passwordError = '';
  passwordSaved = '';

  // ── Two-factor enrollment ──
  enrollStep: 'idle' | 'scan' = 'idle';
  secret = '';
  otpAuthUri = '';
  verifyCode = '';
  isEnrolling = false;
  twoFactorError = '';
  twoFactorMessage = '';

  constructor(
    private readonly authService: AuthService,
    private readonly userService: UserService
  ) {
    this.username = authService.getUsername();
  }

  changePassword(): void {
    const userId = this.authService.getUserId();
    if (!userId || !this.newPassword || this.isSavingPassword) return;
    if (this.newPassword !== this.confirmPassword) {
      this.passwordError = 'Passwords do not match.';
      return;
    }

    this.isSavingPassword = true;
    this.passwordError = '';
    this.passwordSaved = '';
    this.userService.resetPassword(userId, this.newPassword).subscribe({
      next: () => {
        this.isSavingPassword = false;
        this.newPassword = '';
        this.confirmPassword = '';
        this.passwordSaved = 'Password changed.';
      },
      error: (err) => {
        this.isSavingPassword = false;
        this.passwordError = err.error?.message || err.error?.Message || err.error || 'Failed to change password.';
      }
    });
  }

  startEnroll(): void {
    this.twoFactorError = '';
    this.twoFactorMessage = '';
    this.authService.newTwoFactorSecret().subscribe({
      next: (res) => {
        this.secret = res.secret;
        this.otpAuthUri = res.otpAuthUri;
        this.verifyCode = '';
        this.enrollStep = 'scan';
      },
      error: (err) => {
        this.twoFactorError = err.error?.message || err.error?.Message || 'Failed to generate a new secret.';
      }
    });
  }

  confirmEnroll(): void {
    const userId = this.authService.getUserId();
    if (!userId || !this.verifyCode.trim() || this.isEnrolling) return;

    this.isEnrolling = true;
    this.twoFactorError = '';
    this.authService.verifyTwoFactorCode(this.secret, this.verifyCode.trim()).subscribe({
      next: (res) => {
        if (!res.valid) {
          this.isEnrolling = false;
          this.twoFactorError = 'That code doesn\'t match. Check your authenticator app and try again.';
          return;
        }
        this.userService.setTwoFactor(userId, true, this.secret).subscribe({
          next: () => {
            this.isEnrolling = false;
            this.enrollStep = 'idle';
            this.twoFactorMessage = 'Two-factor authentication is now enabled.';
          },
          error: (err) => {
            this.isEnrolling = false;
            this.twoFactorError = err.error?.message || err.error?.Message || 'Failed to enable two-factor authentication.';
          }
        });
      },
      error: (err) => {
        this.isEnrolling = false;
        this.twoFactorError = err.error?.message || err.error?.Message || 'Failed to verify the code.';
      }
    });
  }

  cancelEnroll(): void {
    this.enrollStep = 'idle';
    this.secret = '';
    this.otpAuthUri = '';
    this.verifyCode = '';
  }

  disableTwoFactor(): void {
    const userId = this.authService.getUserId();
    if (!userId) return;
    if (!confirm('Disable two-factor authentication for your account?')) return;

    this.twoFactorError = '';
    this.userService.setTwoFactor(userId, false, null).subscribe({
      next: () => (this.twoFactorMessage = 'Two-factor authentication is now disabled.'),
      error: (err) => {
        this.twoFactorError = err.error?.message || err.error?.Message || 'Failed to disable two-factor authentication.';
      }
    });
  }
}
