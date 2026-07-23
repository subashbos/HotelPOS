import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { AccountComponent } from './account.component';
import { AuthService } from '../../../services/auth.service';
import { UserService } from '../../../services/user.service';

describe('AccountComponent', () => {
  let component: AccountComponent;
  let fixture: ComponentFixture<AccountComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let userServiceSpy: jasmine.SpyObj<UserService>;

  beforeEach(async () => {
    authServiceSpy = jasmine.createSpyObj('AuthService', ['getUsername', 'getUserId']);
    userServiceSpy = jasmine.createSpyObj('UserService', ['resetPassword', 'setTwoFactor']);

    authServiceSpy.getUsername.and.returnValue('testuser');
    authServiceSpy.getUserId.and.returnValue(1);

    await TestBed.configureTestingModule({
      declarations: [AccountComponent],
      imports: [FormsModule],
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        { provide: UserService, useValue: userServiceSpy }
      ]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(AccountComponent);
    component = fixture.componentInstance;
  });

  it('should create component and set username', () => {
    expect(component).toBeTruthy();
    expect(component.username).toBe('testuser');
  });

  it('should validate password mismatch', () => {
    component.newPassword = 'Password123!';
    component.confirmPassword = 'Password456!';

    component.changePassword();

    expect(component.passwordError).toBe('Passwords do not match.');
    expect(userServiceSpy.resetPassword).not.toHaveBeenCalled();
  });

  it('should change password successfully', () => {
    userServiceSpy.resetPassword.and.returnValue(of(void 0));
    component.newPassword = 'Password123!';
    component.confirmPassword = 'Password123!';

    component.changePassword();

    expect(userServiceSpy.resetPassword).toHaveBeenCalledWith(1, 'Password123!');
    expect(component.passwordSaved).toBe('Password changed.');
  });

  it('should not change password if userId is null, password empty, or already saving', () => {
    authServiceSpy.getUserId.and.returnValue(null);
    component.newPassword = 'Password123!';
    component.confirmPassword = 'Password123!';
    component.changePassword();
    expect(userServiceSpy.resetPassword).not.toHaveBeenCalled();

    authServiceSpy.getUserId.and.returnValue(1);
    component.newPassword = '';
    component.changePassword();
    expect(userServiceSpy.resetPassword).not.toHaveBeenCalled();

    component.newPassword = 'Password123!';
    component.isSavingPassword = true;
    component.changePassword();
    expect(userServiceSpy.resetPassword).not.toHaveBeenCalled();
  });

  it('should handle change password error variations', () => {
    userServiceSpy.resetPassword.and.returnValue(throwError(() => ({ error: { message: 'Weak password' } })));
    component.newPassword = 'Password123!';
    component.confirmPassword = 'Password123!';
    component.changePassword();
    expect(component.passwordError).toBe('Weak password');

    userServiceSpy.resetPassword.and.returnValue(throwError(() => ({})));
    component.changePassword();
    expect(component.passwordError).toBe('Failed to change password.');
  });

  it('should start and cancel 2FA enrollment', () => {
    authServiceSpy.newTwoFactorSecret = jasmine.createSpy('newTwoFactorSecret').and.returnValue(of({ secret: 'sec123', otpAuthUri: 'uri123' }));
    component.startEnroll();
    expect(component.secret).toBe('sec123');
    expect(component.otpAuthUri).toBe('uri123');
    expect(component.enrollStep).toBe('scan');

    component.cancelEnroll();
    expect(component.enrollStep).toBe('idle');
    expect(component.secret).toBe('');
  });

  it('should handle startEnroll error', () => {
    authServiceSpy.newTwoFactorSecret = jasmine.createSpy('newTwoFactorSecret').and.returnValue(throwError(() => ({ error: { message: 'Gen error' } })));
    component.startEnroll();
    expect(component.twoFactorError).toBe('Gen error');
  });

  it('should confirm 2FA enrollment valid, invalid code, and error paths', () => {
    authServiceSpy.verifyTwoFactorCode = jasmine.createSpy('verifyTwoFactorCode');
    
    // Guard checks
    component.verifyCode = '   ';
    component.confirmEnroll();
    expect(authServiceSpy.verifyTwoFactorCode).not.toHaveBeenCalled();

    // Invalid code
    component.secret = 'sec123';
    component.verifyCode = '000000';
    authServiceSpy.verifyTwoFactorCode.and.returnValue(of({ valid: false }));
    component.confirmEnroll();
    expect(component.twoFactorError).toContain("doesn't match");

    // Valid code + setTwoFactor success
    authServiceSpy.verifyTwoFactorCode.and.returnValue(of({ valid: true }));
    userServiceSpy.setTwoFactor.and.returnValue(of(void 0 as any));
    component.confirmEnroll();
    expect(component.twoFactorMessage).toBe('Two-factor authentication is now enabled.');

    // setTwoFactor error
    userServiceSpy.setTwoFactor.and.returnValue(throwError(() => ({ error: { message: 'Set error' } })));
    component.confirmEnroll();
    expect(component.twoFactorError).toBe('Set error');

    // verifyTwoFactorCode error
    authServiceSpy.verifyTwoFactorCode.and.returnValue(throwError(() => ({ error: { message: 'Verify error' } })));
    component.confirmEnroll();
    expect(component.twoFactorError).toBe('Verify error');
  });

  it('should handle disableTwoFactor success, cancel, and error', () => {
    const confirmSpy = spyOn(window, 'confirm');

    // Cancelled
    confirmSpy.and.returnValue(false);
    component.disableTwoFactor();
    expect(userServiceSpy.setTwoFactor).not.toHaveBeenCalled();

    // Confirmed + success
    confirmSpy.and.returnValue(true);
    userServiceSpy.setTwoFactor.and.returnValue(of(void 0 as any));
    component.disableTwoFactor();
    expect(component.twoFactorMessage).toBe('Two-factor authentication is now disabled.');

    // Error
    userServiceSpy.setTwoFactor.and.returnValue(throwError(() => ({ error: { message: 'Disable err' } })));
    component.disableTwoFactor();
    expect(component.twoFactorError).toBe('Disable err');
  });
});
