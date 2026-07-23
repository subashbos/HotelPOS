import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { UsersComponent } from './users.component';
import { UserService } from '../../../services/user.service';
import { RoleService } from '../../../services/role.service';
import { AppUser } from '../../../models/user.model';
import { Role } from '../../../models/role.model';

describe('UsersComponent', () => {
  let component: UsersComponent;
  let fixture: ComponentFixture<UsersComponent>;
  let userServiceSpy: jasmine.SpyObj<UserService>;
  let roleServiceSpy: jasmine.SpyObj<RoleService>;

  const mockUsers: AppUser[] = [
    { sNo: 1, id: 1, username: 'admin', role: 'Admin', isActive: true, mustChangePassword: false }
  ];
  const mockRoles: Role[] = [
    { id: 1, name: 'Admin', description: 'Administrator', permissions: [] }
  ];

  beforeEach(async () => {
    userServiceSpy = jasmine.createSpyObj('UserService', [
      'getUsers',
      'createUser',
      'deleteUser',
      'resetPassword',
      'setEmail',
      'toggleActive'
    ]);
    roleServiceSpy = jasmine.createSpyObj('RoleService', ['getRoles']);

    await TestBed.configureTestingModule({
      declarations: [UsersComponent],
      imports: [FormsModule],
      providers: [
        { provide: UserService, useValue: userServiceSpy },
        { provide: RoleService, useValue: roleServiceSpy }
      ]
    }).compileComponents();
  });

  beforeEach(() => {
    userServiceSpy.getUsers.and.returnValue(of(mockUsers));
    roleServiceSpy.getRoles.and.returnValue(of(mockRoles));
    fixture = TestBed.createComponent(UsersComponent);
    component = fixture.componentInstance;
  });

  it('should create component', () => {
    expect(component).toBeTruthy();
  });

  it('should load users and roles on init', () => {
    fixture.detectChanges();

    expect(userServiceSpy.getUsers).toHaveBeenCalled();
    expect(roleServiceSpy.getRoles).toHaveBeenCalled();
    expect().toHaveSize();
    expect().toHaveSize();
    expect(component.isLoading).toBeFalse();
  });

  it('should handle users loading error', () => {
    userServiceSpy.getUsers.and.returnValue(throwError(() => new Error('Error')));

    fixture.detectChanges();

    expect(component.isLoading).toBeFalse();
    expect(component.loadError).toBe('Failed to load users. Please check the server connection.');
  });

  it('should open and close user creation modal', () => {
    component.openAddForm();
    expect(component.showForm).toBeTrue();
    expect(component.newUsername).toBe('');

    component.closeForm();
    expect(component.showForm).toBeFalse();
  });

  it('should create user and reload list', () => {
    userServiceSpy.createUser.and.returnValue(of(void 0));
    component.roles = mockRoles;
    component.openAddForm();
    component.newUsername = 'cashier';
    component.newPassword = 'Password123!';
    component.newRoleId = 1;

    component.createUser();

    expect(userServiceSpy.createUser).toHaveBeenCalled();
    expect(component.showForm).toBeFalse();
  });

  it('should delete user when confirmed', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    userServiceSpy.deleteUser.and.returnValue(of(void 0));

    component.deleteUser(mockUsers[0]);

    expect(userServiceSpy.deleteUser).toHaveBeenCalledWith(1);
  });

  it('should handle role load error on init', () => {
    spyOn(console, 'error');
    roleServiceSpy.getRoles.and.returnValue(throwError(() => new Error('Role error')));
    fixture.detectChanges();
    expect().toHaveSize();
  });

  it('should set newRoleId to null if roles array is empty on openAddForm', () => {
    component.roles = [];
    component.openAddForm();
    expect(component.newRoleId).toBeNull();
  });

  it('should enforce validation guards on createUser', () => {
    component.roles = mockRoles;
    component.openAddForm();

    // empty username
    component.newUsername = '   ';
    component.createUser();
    expect(userServiceSpy.createUser).not.toHaveBeenCalled();

    // empty password
    component.newUsername = 'valid';
    component.newPassword = '';
    component.createUser();
    expect(userServiceSpy.createUser).not.toHaveBeenCalled();

    // null roleId
    component.newPassword = 'pass';
    component.newRoleId = null;
    component.createUser();
    expect(userServiceSpy.createUser).not.toHaveBeenCalled();

    // invalid roleId (not found in roles)
    component.newRoleId = 99;
    component.createUser();
    expect(userServiceSpy.createUser).not.toHaveBeenCalled();

    // isSaving = true guard
    component.newRoleId = 1;
    component.isSaving = true;
    component.createUser();
    expect(userServiceSpy.createUser).not.toHaveBeenCalled();
  });

  it('should handle createUser error response variations', () => {
    spyOn(console, 'error');
    component.roles = mockRoles;
    component.openAddForm();
    component.newUsername = 'valid';
    component.newPassword = 'pass';
    component.newRoleId = 1;

    userServiceSpy.createUser.and.returnValue(throwError(() => ({ error: { message: 'Duplicate user' } })));
    component.createUser();
    expect(component.actionError).toBe('Duplicate user');

    userServiceSpy.createUser.and.returnValue(throwError(() => ({})));
    component.createUser();
    expect(component.actionError).toBe('Failed to create user.');
  });

  it('should toggle active status and handle error', () => {
    spyOn(console, 'error');
    userServiceSpy.toggleActive.and.returnValue(of(void 0));
    component.toggleActive(mockUsers[0]);
    expect(userServiceSpy.toggleActive).toHaveBeenCalledWith(1, false);

    userServiceSpy.toggleActive.and.returnValue(throwError(() => ({ error: { message: 'Toggle fail' } })));
    component.toggleActive(mockUsers[0]);
    expect(component.actionError).toBe('Toggle fail');
  });

  it('should handle password reset workflow and error', () => {
    spyOn(console, 'error');
    component.startReset(mockUsers[0]);
    expect(component.resettingUserId).toBe(1);

    component.cancelReset();
    expect(component.resettingUserId).toBeNull();

    component.startReset(mockUsers[0]);
    component.resetPasswordValue = '';
    component.confirmReset(mockUsers[0]);
    expect(userServiceSpy.resetPassword).not.toHaveBeenCalled();

    component.resetPasswordValue = 'NewPass123';
    userServiceSpy.resetPassword.and.returnValue(of(void 0));
    component.confirmReset(mockUsers[0]);
    expect(userServiceSpy.resetPassword).toHaveBeenCalledWith(1, 'NewPass123');
    expect(component.resettingUserId).toBeNull();

    userServiceSpy.resetPassword.and.returnValue(throwError(() => ({ error: { message: 'Reset fail' } })));
    component.resetPasswordValue = 'NewPass123';
    component.confirmReset(mockUsers[0]);
    expect(component.actionError).toBe('Reset fail');
  });

  it('should handle email edit workflow and error', () => {
    spyOn(console, 'error');
    component.startEditEmail(mockUsers[0]);
    expect(component.editingEmailUserId).toBe(1);

    component.cancelEditEmail();
    expect(component.editingEmailUserId).toBeNull();

    component.startEditEmail(mockUsers[0]);
    component.emailValue = 'test@example.com';
    userServiceSpy.setEmail.and.returnValue(of(void 0));
    component.confirmEditEmail(mockUsers[0]);
    expect(userServiceSpy.setEmail).toHaveBeenCalledWith(1, 'test@example.com');
    expect(component.editingEmailUserId).toBeNull();

    userServiceSpy.setEmail.and.returnValue(throwError(() => ({ error: { message: 'Email fail' } })));
    component.confirmEditEmail(mockUsers[0]);
    expect(component.actionError).toBe('Email fail');
  });

  it('should cancel delete when confirm returns false and handle delete error', () => {
    spyOn(console, 'error');
    const confirmSpy = spyOn(window, 'confirm');
    confirmSpy.and.returnValue(false);
    component.deleteUser(mockUsers[0]);
    expect(userServiceSpy.deleteUser).not.toHaveBeenCalled();

    confirmSpy.and.returnValue(true);
    userServiceSpy.deleteUser.and.returnValue(throwError(() => ({ error: { message: 'Delete fail' } })));
    component.deleteUser(mockUsers[0]);
    expect(component.actionError).toBe('Delete fail');
  });

  it('should track user by id', () => {
    expect(component.trackByUserId(0, mockUsers[0])).toBe(1);
  });
});
