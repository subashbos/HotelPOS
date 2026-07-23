import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { SettingsService } from './settings.service';
import { environment } from '../../environments/environment';
import { SaveSettingsRequest, SystemSettings } from '../models/settings.model';

describe('SettingsService', () => {
  let service: SettingsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [SettingsService, provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(SettingsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getSettings', () => {
    it('should retrieve system settings', () => {
      const dummySettings: SystemSettings = {
        hotelName: 'Grand Hotel', hotelAddress: '123 Main St', hotelPhone: '1234567890', hotelGst: 'GST123',
        defaultPrinter: 'Printer1', showPrintPreview: true, receiptFormat: 'A4',
        showGstBreakdown: true, showItemsOnBill: true, showDiscountLine: false, showPhoneOnReceipt: true,
        showThankYouFooter: true, enableRoundOff: true, isCompositionScheme: false,
        enableAutomatedBackups: false, idleTimeoutMinutes: 15,
        smtpPort: 587, smtpPasswordSet: false, smtpUseSsl: true
      };

      service.getSettings().subscribe(settings => {
        expect(settings).toEqual(dummySettings);
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/settings`);
      expect(req.request.method).toBe('GET');
      req.flush(dummySettings);
    });
  });

  describe('saveSettings', () => {
    it('should save settings via PUT', () => {
      const request: SaveSettingsRequest = {
        hotelName: 'Grand Hotel', hotelAddress: '123 Main St', hotelPhone: '1234567890', hotelGst: 'GST123',
        defaultPrinter: 'Printer1', showPrintPreview: true, receiptFormat: 'A4',
        showGstBreakdown: true, showItemsOnBill: true, showDiscountLine: false, showPhoneOnReceipt: true,
        showThankYouFooter: true, enableRoundOff: true, isCompositionScheme: false,
        enableAutomatedBackups: false, idleTimeoutMinutes: 15,
        smtpPort: 587, smtpUseSsl: true, smtpPassword: 'newpassword'
      };

      service.saveSettings(request).subscribe(response => {
        expect(response).toBeNull();
      });

      const req = httpMock.expectOne(`${environment.apiBaseUrl}/settings`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(request);
      req.flush(null);
    });
  });
});
