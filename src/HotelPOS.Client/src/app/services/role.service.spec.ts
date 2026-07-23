import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { RoleService } from './role.service';
import { environment } from '../../environments/environment';
import { Role, RolePermission } from '../models/role.model';

describe('RoleService', () => {
  let service: RoleService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [RoleService, provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(RoleService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getRoles', () => {
    it('should retrieve all roles', () => {
      const dummyRoles: Role[] = [
        { id: 1, name: 'Admin', description: 'Full access', permissions: [] },
        { id: 2, name: 'Cashier', description: 'POS access only', permissions: [] }
      ];

      service.getRoles().subscribe(roles => {
        expect(roles).toHaveSize(2);
        expect(roles).toEqual(dummyRoles);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/roles`);
      expect(req.request.method).toBe('GET');
      req.flush(dummyRoles);
    });
  });

  describe('getRole', () => {
    it('should retrieve a single role by id', () => {
      const dummyRole: Role = { id: 1, name: 'Admin', description: 'Full access', permissions: [] };

      service.getRole(1).subscribe(role => {
        expect(role).toEqual(dummyRole);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/roles/1`);
      expect(req.request.method).toBe('GET');
      req.flush(dummyRole);
    });
  });

  describe('createRole', () => {
    it('should create a new role via POST', () => {
      service.createRole('Manager', 'Manages daily operations').subscribe(response => {
        expect(response).toBeNull();
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/roles`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ name: 'Manager', description: 'Manages daily operations' });
      req.flush(null);
    });
  });

  describe('updatePermissions', () => {
    it('should update role permissions via PUT', () => {
      const permissions: RolePermission[] = [
        { id: 1, roleId: 1, moduleName: 'Billing', canAccess: true, canEdit: true, canDelete: false }
      ];

      service.updatePermissions(1, permissions).subscribe(response => {
        expect(response).toBeNull();
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/roles/1/permissions`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(permissions);
      req.flush(null);
    });
  });

  describe('deleteRole', () => {
    it('should delete a role via DELETE', () => {
      service.deleteRole(1).subscribe(response => {
        expect(response).toBeNull();
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/roles/1`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });
  });
});
