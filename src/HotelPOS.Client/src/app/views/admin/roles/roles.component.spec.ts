import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { RolesComponent } from './roles.component';
import { RoleService } from '../../../services/role.service';
import { Role } from '../../../models/role.model';

describe('RolesComponent', () => {
  let component: RolesComponent;
  let fixture: ComponentFixture<RolesComponent>;
  let roleServiceSpy: jasmine.SpyObj<RoleService>;

  const mockRole: Role = {
    id: 1,
    name: 'Manager',
    description: 'Store manager',
    permissions: []
  };

  beforeEach(async () => {
    roleServiceSpy = jasmine.createSpyObj('RoleService', ['getRoles', 'createRole', 'deleteRole', 'updatePermissions']);
    roleServiceSpy.getRoles.and.returnValue(of([mockRole]));

    await TestBed.configureTestingModule({
      declarations: [RolesComponent],
      imports: [FormsModule],
      providers: [
        { provide: RoleService, useValue: roleServiceSpy }
      ]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(RolesComponent);
    component = fixture.componentInstance;
  });

  it('should create component and load roles', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
    expect(roleServiceSpy.getRoles).toHaveBeenCalled();
    expect().toHaveSize();
  });

  it('should handle roles load error', () => {
    roleServiceSpy.getRoles.and.returnValue(throwError(() => new Error('Load error')));
    fixture.detectChanges();
    expect(component.isLoading).toBeFalse();
    expect(component.loadError).toBe('Failed to load roles. Please check the server connection.');
  });

  it('should open and close create form', () => {
    component.openCreateForm();
    expect(component.showCreateForm).toBeTrue();
    expect(component.newRoleName).toBe('');

    component.closeCreateForm();
    expect(component.showCreateForm).toBeFalse();
  });

  it('should create new role', () => {
    roleServiceSpy.createRole.and.returnValue(of(void 0));
    component.openCreateForm();
    component.newRoleName = 'Cashier';
    component.newRoleDescription = 'Counter staff';

    component.createRole();

    expect(roleServiceSpy.createRole).toHaveBeenCalledWith('Cashier', 'Counter staff');
    expect(component.showCreateForm).toBeFalse();
  });

  it('should edit, close, and save permissions', () => {
    roleServiceSpy.updatePermissions.and.returnValue(of(void 0));
    component.editPermissions(mockRole);
    expect(component.editingRole).toEqual(mockRole);
    expect(component.editingPermissions.length).toBeGreaterThan(0);

    component.savePermissions();
    expect(roleServiceSpy.updatePermissions).toHaveBeenCalled();
    expect(component.editingRole).toBeNull();

    component.editPermissions(mockRole);
    component.closePermissionsEditor();
    expect(component.editingRole).toBeNull();
  });

  it('should delete role when confirmed', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    roleServiceSpy.deleteRole.and.returnValue(of(void 0));

    component.deleteRole(mockRole);

    expect(roleServiceSpy.deleteRole).toHaveBeenCalledWith(1);
  });

  it('should track role by id', () => {
    expect(component.trackByRoleId(0, mockRole)).toBe(1);
  });
});
