import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { UserService } from './user.service';
import { environment } from '../../environments/environment';
import { AppUser } from '../models/user.model';

describe('UserService', () => {
  let service: UserService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [UserService, provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(UserService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getUsers', () => {
    it('should retrieve all users', () => {
      const dummyUsers: AppUser[] = [
        { sNo: 1, id: 1, username: 'admin', role: 'Admin', roleId: 1, isActive: true, mustChangePassword: false },
        { sNo: 2, id: 2, username: 'cashier', role: 'Cashier', roleId: 2, isActive: true, mustChangePassword: true }
      ];

      service.getUsers().subscribe(users => {
        expect(users).toHaveSize(2);
        expect(users).toEqual(dummyUsers);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/users`);
      expect(req.request.method).toBe('GET');
      req.flush(dummyUsers);
    });
  });

  describe('createUser', () => {
    it('should create a new user via POST', () => {
      service.createUser('newuser', 'password123', 'Manager', 3).subscribe(response => {
        expect(response).toBeNull();
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/users`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({
        username: 'newuser',
        password: 'password123',
        role: 'Manager',
        roleId: 3
      });
      req.flush(null);
    });
  });

  describe('toggleActive', () => {
    it('should toggle user active status', () => {
      service.toggleActive(1, false).subscribe(response => {
        expect(response).toBeNull();
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/users/1/toggle-active`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ isActive: false });
      req.flush(null);
    });

    it('should activate a user', () => {
      service.toggleActive(2, true).subscribe();

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/users/2/toggle-active`);
      expect(req.request.body).toEqual({ isActive: true });
      req.flush(null);
    });
  });

  describe('resetPassword', () => {
    it('should reset user password via POST', () => {
      service.resetPassword(1, 'newPassword123').subscribe(response => {
        expect(response).toBeNull();
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/users/1/reset-password`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ newPassword: 'newPassword123' });
      req.flush(null);
    });
  });

  describe('setTwoFactor', () => {
    it('should enable two-factor authentication with secret', () => {
      service.setTwoFactor(1, true, 'secret-code-123').subscribe();

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/users/1/two-factor`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ enabled: true, secret: 'secret-code-123' });
      req.flush(null);
    });

    it('should disable two-factor authentication', () => {
      service.setTwoFactor(1, false, null).subscribe();

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/users/1/two-factor`);
      expect(req.request.body).toEqual({ enabled: false, secret: null });
      req.flush(null);
    });
  });

  describe('setEmail', () => {
    it('should set user email', () => {
      service.setEmail(1, 'user@example.com').subscribe();

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/users/1/set-email`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ email: 'user@example.com' });
      req.flush(null);
    });

    it('should clear user email', () => {
      service.setEmail(1, null).subscribe();

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/users/1/set-email`);
      expect(req.request.body).toEqual({ email: null });
      req.flush(null);
    });
  });

  describe('deleteUser', () => {
    it('should delete a user via DELETE', () => {
      service.deleteUser(1).subscribe(response => {
        expect(response).toBeNull();
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/users/1`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });
  });
});
