import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { SettingsComponent } from './settings.component';
import { SettingsService } from '../../../services/settings.service';
import { SystemSettings } from '../../../models/settings.model';

describe('SettingsComponent', () => {
  let component: SettingsComponent;
  let fixture: ComponentFixture<SettingsComponent>;
  let settingsServiceSpy: jasmine.SpyObj<SettingsService>;

  const mockSettings: SystemSettings = {
    hotelName: 'Hotel POS Resto',
    hotelAddress: '123 Main St',
    hotelPhone: '9876543210',
    hotelGst: '29ABCDE1234F1Z5',
    defaultPrinter: 'Thermal Printer',
    showPrintPreview: true,
    receiptFormat: 'Standard',
    showGstBreakdown: true,
    showItemsOnBill: true,
    showDiscountLine: true,
    showPhoneOnReceipt: true,
    showThankYouFooter: true,
    enableRoundOff: true,
    isCompositionScheme: false,
    enableAutomatedBackups: false,
    idleTimeoutMinutes: 30,
    smtpPort: 587,
    smtpPasswordSet: true,
    smtpUseSsl: true
  };

  beforeEach(async () => {
    settingsServiceSpy = jasmine.createSpyObj('SettingsService', ['getSettings', 'saveSettings']);

    await TestBed.configureTestingModule({
      declarations: [SettingsComponent],
      imports: [FormsModule],
      providers: [
        { provide: SettingsService, useValue: settingsServiceSpy }
      ]
    }).compileComponents();
  });

  beforeEach(() => {
    settingsServiceSpy.getSettings.and.returnValue(of(mockSettings));
    fixture = TestBed.createComponent(SettingsComponent);
    component = fixture.componentInstance;
  });

  it('should create component', () => {
    expect(component).toBeTruthy();
  });

  it('should load settings on init', () => {
    fixture.detectChanges();

    expect(settingsServiceSpy.getSettings).toHaveBeenCalled();
    expect(component.settings?.hotelName).toBe('Hotel POS Resto');
    expect(component.isLoading).toBeFalse();
  });

  it('should handle settings load failure', () => {
    settingsServiceSpy.getSettings.and.returnValue(throwError(() => new Error('Load failed')));

    fixture.detectChanges();

    expect(component.isLoading).toBeFalse();
    expect(component.loadError).toBe('Failed to load settings. Please check the server connection.');
  });

  it('should save settings successfully', () => {
    settingsServiceSpy.saveSettings.and.returnValue(of(void 0));
    fixture.detectChanges();

    component.save();

    expect(settingsServiceSpy.saveSettings).toHaveBeenCalled();
    expect(component.isSaving).toBeFalse();
    expect(component.savedMessage).toBe('Settings saved.');
  });

  it('should handle save error', () => {
    settingsServiceSpy.saveSettings.and.returnValue(throwError(() => ({ error: { message: 'Save error' } })));
    fixture.detectChanges();

    component.save();

    expect(component.isSaving).toBeFalse();
    expect(component.actionError).toBe('Save error');
  });
});
