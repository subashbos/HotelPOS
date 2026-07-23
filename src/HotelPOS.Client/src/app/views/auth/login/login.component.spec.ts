import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { of, throwError } from 'rxjs';
import { LoginComponent } from './login.component';
import { AuthService } from '../../../services/auth.service';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let router: Router;

  beforeEach(async () => {
    authServiceSpy = jasmine.createSpyObj('AuthService', ['isLoggedIn', 'login']);
    authServiceSpy.isLoggedIn.and.returnValue(false);

    await TestBed.configureTestingModule({
      declarations: [LoginComponent],
      imports: [FormsModule, RouterTestingModule],
      providers: [
        { provide: AuthService, useValue: authServiceSpy }
      ]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    spyOn(router, 'navigate');
  });

  it('should create component', () => {
    expect(component).toBeTruthy();
  });

  it('should validate empty username or password', () => {
    component.credentials = { username: '', password: '' };
    component.onSubmit();
    expect(component.errorMessage).toBe('Please enter both username and password.');
  });

  it('should login successfully and navigate to admin dashboard', () => {
    authServiceSpy.login.and.returnValue(of({ token: 'jwt-token', refreshToken: 'refresh-token', username: 'admin', role: 'Admin' }));
    component.credentials = { username: 'admin', password: 'password123' };

    component.onSubmit();

    expect(authServiceSpy.login).toHaveBeenCalledWith({ username: 'admin', password: 'password123' });
    expect(router.navigate).toHaveBeenCalledWith(['/admin/dashboard']);
  });

  it('should handle 401 two-factor requirement', () => {
    authServiceSpy.login.and.returnValue(throwError(() => ({ status: 401, error: { requiresTwoFactor: true } })));
    component.credentials = { username: 'admin', password: 'password123' };

    component.onSubmit();

    expect(component.requiresTwoFactor).toBeTrue();
    expect(component.errorMessage).toBe('Enter the 6-digit code from your authenticator app.');
  });

  it('should reset two factor state via useDifferentAccount', () => {
    component.requiresTwoFactor = true;
    component.totpCode = '123456';
    component.useDifferentAccount();

    expect(component.requiresTwoFactor).toBeFalse();
    expect(component.totpCode).toBe('');
  });
});
