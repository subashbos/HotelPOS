import { TestBed, ComponentFixture } from '@angular/core/testing';
import { Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { of, throwError } from 'rxjs';
import { LoginComponent } from './login.component';
import { AuthService } from '../../../services/auth.service';
import { FormsModule } from '@angular/forms';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let router: Router;

  beforeEach(async () => {
    authServiceSpy = jasmine.createSpyObj('AuthService', ['isLoggedIn', 'login']);

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

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should redirect to dashboard on init if already logged in', () => {
    authServiceSpy.isLoggedIn.and.returnValue(true);
    fixture.detectChanges(); // triggers ngOnInit

    expect(router.navigate).toHaveBeenCalledWith(['/admin/dashboard']);
  });

  it('should not redirect on init if not logged in', () => {
    authServiceSpy.isLoggedIn.and.returnValue(false);
    fixture.detectChanges();

    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('should validate form fields on submit', () => {
    authServiceSpy.isLoggedIn.and.returnValue(false);
    fixture.detectChanges();

    component.credentials.username = '';
    component.credentials.password = '';
    component.onSubmit();

    expect(component.errorMessage).toBe('Please enter both username and password.');
    expect(authServiceSpy.login).not.toHaveBeenCalled();
  });

  it('should validate totp code if requiresTwoFactor is true', () => {
    authServiceSpy.isLoggedIn.and.returnValue(false);
    fixture.detectChanges();

    component.credentials.username = 'admin';
    component.credentials.password = 'password';
    component.requiresTwoFactor = true;
    component.totpCode = '';
    component.onSubmit();

    expect(component.errorMessage).toBe('Enter your 6-digit authentication code.');
    expect(authServiceSpy.login).not.toHaveBeenCalled();
  });

  it('should call login and navigate to dashboard on success', () => {
    authServiceSpy.isLoggedIn.and.returnValue(false);
    fixture.detectChanges();

    component.credentials.username = 'admin';
    component.credentials.password = 'password';
    authServiceSpy.login.and.returnValue(of({ token: 'tok', refreshToken: 'ref', username: 'admin', role: 'Admin' }));

    component.onSubmit();

    expect(authServiceSpy.login).toHaveBeenCalledWith({ username: 'admin', password: 'password' });
    expect(component.isLoading).toBeFalse();
    expect(router.navigate).toHaveBeenCalledWith(['/admin/dashboard']);
  });

  it('should handle 401 requiring 2FA', () => {
    authServiceSpy.isLoggedIn.and.returnValue(false);
    fixture.detectChanges();

    component.credentials.username = 'admin';
    component.credentials.password = 'password';
    const errRes = { status: 401, error: { requiresTwoFactor: true } };
    authServiceSpy.login.and.returnValue(throwError(() => errRes));

    component.onSubmit();

    expect(component.isLoading).toBeFalse();
    expect(component.requiresTwoFactor).toBeTrue();
    expect(component.totpCode).toBe('');
    expect(component.errorMessage).toBe('Enter the 6-digit code from your authenticator app.');
  });

  it('should handle 401 invalid credentials', () => {
    authServiceSpy.isLoggedIn.and.returnValue(false);
    fixture.detectChanges();

    component.credentials.username = 'admin';
    component.credentials.password = 'password';
    const errRes = { status: 401, error: null };
    authServiceSpy.login.and.returnValue(throwError(() => errRes));

    component.onSubmit();

    expect(component.isLoading).toBeFalse();
    expect(component.errorMessage).toBe('Invalid username or password.');
  });

  it('should handle general error message from backend', () => {
    authServiceSpy.isLoggedIn.and.returnValue(false);
    fixture.detectChanges();

    component.credentials.username = 'admin';
    component.credentials.password = 'password';
    const errRes = { status: 400, error: { message: 'Some specific error' } };
    authServiceSpy.login.and.returnValue(throwError(() => errRes));

    component.onSubmit();

    expect(component.isLoading).toBeFalse();
    expect(component.errorMessage).toBe('Some specific error');
  });

  it('should handle unexpected errors', () => {
    authServiceSpy.isLoggedIn.and.returnValue(false);
    fixture.detectChanges();

    component.credentials.username = 'admin';
    component.credentials.password = 'password';
    const errRes = { status: 500, error: null };
    authServiceSpy.login.and.returnValue(throwError(() => errRes));

    component.onSubmit();

    expect(component.isLoading).toBeFalse();
    expect(component.errorMessage).toBe('An unexpected error occurred. Please try again.');
  });

  it('should submit with totpCode if requiresTwoFactor is true', () => {
    authServiceSpy.isLoggedIn.and.returnValue(false);
    fixture.detectChanges();

    component.credentials.username = 'admin';
    component.credentials.password = 'password';
    component.requiresTwoFactor = true;
    component.totpCode = '123456';
    authServiceSpy.login.and.returnValue(of({ token: 'tok', refreshToken: 'ref', username: 'admin', role: 'Admin' }));

    component.onSubmit();

    expect(authServiceSpy.login).toHaveBeenCalledWith({ username: 'admin', password: 'password', totpCode: '123456' });
  });

  it('should reset state when useDifferentAccount is called', () => {
    component.requiresTwoFactor = true;
    component.totpCode = '123456';
    component.credentials.password = 'somepassword';
    component.errorMessage = 'error';

    component.useDifferentAccount();

    expect(component.requiresTwoFactor).toBeFalse();
    expect(component.totpCode).toBe('');
    expect(component.credentials.password).toBe('');
    expect(component.errorMessage).toBe('');
  });
});
