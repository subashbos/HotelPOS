import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { of, throwError } from 'rxjs';
import { ForgotPasswordComponent } from './forgot-password.component';
import { AuthService } from '../../../services/auth.service';

describe('ForgotPasswordComponent', () => {
  let component: ForgotPasswordComponent;
  let fixture: ComponentFixture<ForgotPasswordComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let router: Router;

  beforeEach(async () => {
    authServiceSpy = jasmine.createSpyObj('AuthService', ['forgotPassword', 'resetPasswordWithCode']);

    await TestBed.configureTestingModule({
      declarations: [ForgotPasswordComponent],
      imports: [FormsModule, RouterTestingModule],
      providers: [
        { provide: AuthService, useValue: authServiceSpy }
      ]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(ForgotPasswordComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    spyOn(router, 'navigate');
  });

  it('should create component', () => {
    expect(component).toBeTruthy();
  });

  it('should request reset code successfully', () => {
    authServiceSpy.forgotPassword.and.returnValue(of(void 0));
    component.username = 'john';

    component.requestCode();

    expect(authServiceSpy.forgotPassword).toHaveBeenCalledWith('john');
    expect(component.step).toBe('confirm');
  });

  it('should confirm reset password successfully', () => {
    authServiceSpy.resetPasswordWithCode.and.returnValue(of(void 0));
    component.username = 'john';
    component.code = '123456';
    component.newPassword = 'Password123!';
    component.confirmPassword = 'Password123!';

    component.confirmReset();

    expect(authServiceSpy.resetPasswordWithCode).toHaveBeenCalledWith('john', '123456', 'Password123!');
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('should validate password mismatch in confirmReset', () => {
    component.code = '123456';
    component.newPassword = 'Password123!';
    component.confirmPassword = 'Password456!';

    component.confirmReset();

    expect(component.errorMessage).toBe('Passwords do not match.');
  });

  it('should go back to request step', () => {
    component.step = 'confirm';
    component.backToRequest();
    expect(component.step).toBe('request');
  });
});
