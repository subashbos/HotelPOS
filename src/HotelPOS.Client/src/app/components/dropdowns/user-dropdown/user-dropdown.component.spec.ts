import { TestBed, ComponentFixture } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { UserDropdownComponent } from './user-dropdown.component';
import { AuthService } from '../../../services/auth.service';

describe('UserDropdownComponent', () => {
  let component: UserDropdownComponent;
  let fixture: ComponentFixture<UserDropdownComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;

  beforeEach(async () => {
    authServiceSpy = jasmine.createSpyObj('AuthService', ['getUsername', 'logout']);
    authServiceSpy.getUsername.and.returnValue('testuser');

    await TestBed.configureTestingModule({
      declarations: [UserDropdownComponent],
      imports: [RouterTestingModule],
      providers: [
        { provide: AuthService, useValue: authServiceSpy }
      ]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(UserDropdownComponent);
    component = fixture.componentInstance;
  });

  it('should create component and return username', () => {
    expect(component).toBeTruthy();
    expect(component.username).toBe('testuser');
  });

  it('should call logout and navigate to login', () => {
    const event = new MouseEvent('click');
    spyOn(event, 'preventDefault');

    component.logout(event);

    expect(authServiceSpy.logout).toHaveBeenCalled();
    expect(component.dropdownPopoverShow).toBeFalse();
  });

  it('should toggle dropdown open and closed', () => {
    const event = new MouseEvent('click');
    spyOn(event, 'preventDefault');

    expect(component.dropdownPopoverShow).toBeFalse();
    component.toggleDropdown(event);
    expect(component.dropdownPopoverShow).toBeTrue();

    component.toggleDropdown(event);
    expect(component.dropdownPopoverShow).toBeFalse();
  });

  it('should initialize popper on ngAfterViewInit', () => {
    component.btnDropdownRef = { nativeElement: document.createElement('a') };
    component.popoverDropdownRef = { nativeElement: document.createElement('div') };
    expect(() => component.ngAfterViewInit()).not.toThrow();
  });
});
