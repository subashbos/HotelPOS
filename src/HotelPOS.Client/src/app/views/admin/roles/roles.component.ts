import { Component, OnInit } from '@angular/core';
import { RoleService } from '../../../services/role.service';
import { PERMISSION_MODULES, Role, RolePermission } from '../../../models/role.model';

@Component({
  standalone: false,
  selector: 'app-roles',
  templateUrl: './roles.component.html',
})
export class RolesComponent implements OnInit {
  roles: Role[] = [];
  isLoading = false;
  loadError = '';
  actionError = '';
  isSaving = false;

  readonly modules = PERMISSION_MODULES;

  showCreateForm = false;
  newRoleName = '';
  newRoleDescription = '';

  editingRole: Role | null = null;
  editingPermissions: RolePermission[] = [];

  constructor(private readonly roleService: RoleService) {}

  ngOnInit(): void {
    this.loadRoles();
  }

  loadRoles(): void {
    this.isLoading = true;
    this.loadError = '';
    this.roleService.getRoles().subscribe({
      next: (roles) => {
        this.roles = roles;
        this.isLoading = false;
      },
      error: (err) => {
        this.loadError = 'Failed to load roles. Please check the server connection.';
        this.isLoading = false;
        console.error('Roles load error:', err);
      }
    });
  }

  openCreateForm(): void {
    this.newRoleName = '';
    this.newRoleDescription = '';
    this.actionError = '';
    this.showCreateForm = true;
  }

  closeCreateForm(): void {
    this.showCreateForm = false;
  }

  createRole(): void {
    if (!this.newRoleName.trim() || this.isSaving) return;
    this.isSaving = true;
    this.actionError = '';
    this.roleService.createRole(this.newRoleName.trim(), this.newRoleDescription.trim()).subscribe({
      next: () => {
        this.isSaving = false;
        this.closeCreateForm();
        this.loadRoles();
      },
      error: (err) => {
        this.isSaving = false;
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to create role.';
        console.error('Role create error:', err);
      }
    });
  }

  editPermissions(role: Role): void {
    this.editingRole = role;
    // Ensure every module has a permission row so checkboxes always render.
    this.editingPermissions = this.modules.map((moduleName) => {
      const existing = role.permissions.find((p) => p.moduleName === moduleName);
      return existing ?? { id: 0, roleId: role.id, moduleName, canAccess: false, canEdit: false, canDelete: false };
    });
    this.actionError = '';
  }

  closePermissionsEditor(): void {
    this.editingRole = null;
  }

  savePermissions(): void {
    if (!this.editingRole || this.isSaving) return;
    this.isSaving = true;
    this.actionError = '';
    this.roleService.updatePermissions(this.editingRole.id, this.editingPermissions).subscribe({
      next: () => {
        this.isSaving = false;
        this.closePermissionsEditor();
        this.loadRoles();
      },
      error: (err) => {
        this.isSaving = false;
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to save permissions.';
        console.error('Permissions save error:', err);
      }
    });
  }

  deleteRole(role: Role): void {
    if (!confirm(`Delete role "${role.name}"?`)) return;
    this.roleService.deleteRole(role.id).subscribe({
      next: () => this.loadRoles(),
      error: (err) => {
        this.actionError = err.error?.message || err.error?.Message || err.error || 'Failed to delete role.';
        console.error('Role delete error:', err);
      }
    });
  }

  trackByRoleId(_index: number, role: Role): number {
    return role.id;
  }
}
