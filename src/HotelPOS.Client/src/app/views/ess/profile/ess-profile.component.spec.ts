import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { EssProfileComponent } from './ess-profile.component';
import { EssService } from '../../../services/ess.service';
import { Employee } from '../../../models/employee.model';

describe('EssProfileComponent', () => {
  let component: EssProfileComponent;
  let fixture: ComponentFixture<EssProfileComponent>;
  let essServiceSpy: jasmine.SpyObj<EssService>;

  const mockProfile: Employee = {
    id: 1, employeeCode: 'E001', firstName: 'Self', phone: '9999999999', address: 'Old Address',
    emergencyContactName: 'Kin', emergencyContactPhone: '8888888888'
  } as Employee;

  beforeEach(async () => {
    essServiceSpy = jasmine.createSpyObj('EssService', ['getProfile', 'updateProfile']);
    essServiceSpy.getProfile.and.returnValue(of(mockProfile));

    await TestBed.configureTestingModule({
      declarations: [EssProfileComponent],
      imports: [FormsModule],
      providers: [{ provide: EssService, useValue: essServiceSpy }]
    }).compileComponents();

    fixture = TestBed.createComponent(EssProfileComponent);
    component = fixture.componentInstance;
  });

  it('should create and load the profile, pre-filling the form', () => {
    fixture.detectChanges();

    expect(component).toBeTruthy();
    expect(component.profile).toEqual(mockProfile);
    expect(component.formPhone).toBe('9999999999');
    expect(component.formAddress).toBe('Old Address');
    expect(component.isLoading).toBeFalse();
  });

  it('should default form fields to empty strings when profile fields are missing', () => {
    essServiceSpy.getProfile.and.returnValue(of({ id: 1, employeeCode: 'E001', firstName: 'Self' } as Employee));

    fixture.detectChanges();

    expect(component.formPhone).toBe('');
    expect(component.formAddress).toBe('');
  });

  it('should handle load error', () => {
    spyOn(console, 'error');
    essServiceSpy.getProfile.and.returnValue(throwError(() => new Error('boom')));

    fixture.detectChanges();

    expect(component.loadError).toContain('Failed to load your profile');
  });

  it('should save and show a success message', () => {
    fixture.detectChanges();
    essServiceSpy.updateProfile.and.returnValue(of(void 0));
    component.formPhone = '7777777777';

    component.save();

    expect(essServiceSpy.updateProfile).toHaveBeenCalledWith({
      phone: '7777777777',
      address: 'Old Address',
      emergencyContactName: 'Kin',
      emergencyContactPhone: '8888888888'
    });
    expect(component.successMessage).toBe('Profile updated.');
    expect(component.isSaving).toBeFalse();
  });

  it('should not save while already saving', () => {
    fixture.detectChanges();
    component.isSaving = true;

    component.save();

    expect(essServiceSpy.updateProfile).not.toHaveBeenCalled();
  });

  it('should surface save errors', () => {
    spyOn(console, 'error');
    fixture.detectChanges();
    essServiceSpy.updateProfile.and.returnValue(throwError(() => ({ error: { message: 'Update failed' } })));

    component.save();

    expect(component.actionError).toBe('Update failed');
    expect(component.isSaving).toBeFalse();
  });
});
