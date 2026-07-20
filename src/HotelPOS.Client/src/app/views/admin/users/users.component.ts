import { Component, OnInit } from '@angular/core';
import { UserService } from '../../../services/user.service';
import { RoleService } from '../../../services/role.service';
import { AppUser } from '../../../models/user.model';
import { Role } from '../../../models/role.model';

@Component({
  standalone: false,
  selector: 'app-users',
  templateUrl: './users.component.html',
})
export class UsersComponent implements OnInit {
  users: AppUser[] = [];
  roles: Role[] = [];

  isLoading = false;
  loadError = '';
  actionError = '';
  isSaving = false;

  showForm = false;
  newUsername = '';
  newPassword = '';
  newRoleId: number | null = null;

  resettingUserId: number | null = null;
  resetPasswordValue = '';

  editingEmailUserId: number | null = null;
  emailValue = '';

  constructor(
    private readonly userService: UserService,
    private readonly roleService: RoleService
  ) {}

  ngOnInit(): void {
    this.loadUsers();
    this.roleService.getRoles().subscribe({
      next: (roles) => (this.roles = roles),
      error: (err) => console.error('Roles load error:', err)
    });
  }

  loadUsers(): void {
    this.isLoading = true;
    this.loadError = '';
    this.userService.getUsers().subscribe({
      next: (users) => {
        this.users = users;
        this.isLoading = false;
      },
      error: (err) => {
        this.loadError = 'Failed to load users. Please check the server connection.';
        this.isLoading = false;
        console.error('Users load error:', err);
      }
    });
  }

  openAddForm(): void {
    this.newUsername = '';
    this.newPassword = '';
    this.newRoleId = this.roles.length > 0 ? this.roles[0].id : null;
    this.actionError = '';
    this.showForm = true;
  }

  closeForm(): void {
    this.showForm = false;
  }

  createUser(): void {
    if (!this.newUsername.trim() || !this.newPassword || !this.newRoleId || this.isSaving) return;
    const role = this.roles.find((r) => r.id === this.newRoleId);
    if (!role) return;

    this.isSaving = true;
    this.actionError = '';
    this.userService.createUser(this.newUsername.trim(), this.newPassword, role.name, role.id).subscribe({
      next: () => {
        this.isSaving = false;
        this.closeForm();
        this.loadUsers();
      },
      error: (err) => {
        this.isSaving = false;
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to create user.';
        console.error('User create error:', err);
      }
    });
  }

  toggleActive(user: AppUser): void {
    this.userService.toggleActive(user.id, !user.isActive).subscribe({
      next: () => this.loadUsers(),
      error: (err) => {
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to update user status.';
        console.error('Toggle active error:', err);
      }
    });
  }

  startReset(user: AppUser): void {
    this.resettingUserId = user.id;
    this.resetPasswordValue = '';
  }

  cancelReset(): void {
    this.resettingUserId = null;
  }

  confirmReset(user: AppUser): void {
    if (!this.resetPasswordValue) return;
    this.userService.resetPassword(user.id, this.resetPasswordValue).subscribe({
      next: () => {
        this.resettingUserId = null;
      },
      error: (err) => {
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to reset password.';
        console.error('Reset password error:', err);
      }
    });
  }

  startEditEmail(user: AppUser): void {
    this.editingEmailUserId = user.id;
    this.emailValue = '';
  }

  cancelEditEmail(): void {
    this.editingEmailUserId = null;
  }

  confirmEditEmail(user: AppUser): void {
    this.userService.setEmail(user.id, this.emailValue || null).subscribe({
      next: () => {
        this.editingEmailUserId = null;
      },
      error: (err) => {
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to set email.';
        console.error('Set email error:', err);
      }
    });
  }

  deleteUser(user: AppUser): void {
    if (!confirm(`Delete user "${user.username}"?`)) return;
    this.userService.deleteUser(user.id).subscribe({
      next: () => this.loadUsers(),
      error: (err) => {
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to delete user.';
        console.error('User delete error:', err);
      }
    });
  }

  trackByUserId(_index: number, user: AppUser): number {
    return user.id;
  }
}
