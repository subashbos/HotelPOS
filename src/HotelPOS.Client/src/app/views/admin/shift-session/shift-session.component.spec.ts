import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { ShiftSessionComponent } from './shift-session.component';
import { CashSessionService } from '../../../services/cash-session.service';
import { CashSession } from '../../../models/cash-session.model';

describe('ShiftSessionComponent', () => {
  let component: ShiftSessionComponent;
  let fixture: ComponentFixture<ShiftSessionComponent>;
  let cashSessionServiceSpy: jasmine.SpyObj<CashSessionService>;

  const mockSession: CashSession = {
    sNo: 1,
    id: 1,
    openedAt: '2026-01-22T08:00:00Z',
    openingBalance: 5000,
    openedBy: 'admin',
    status: 'Open'
  };

  beforeEach(async () => {
    cashSessionServiceSpy = jasmine.createSpyObj('CashSessionService', [
      'getCurrentSession',
      'getCurrentSessionSalesTotal',
      'getHistory',
      'openSession',
      'closeSession'
    ]);

    cashSessionServiceSpy.getCurrentSession.and.returnValue(of(mockSession));
    cashSessionServiceSpy.getCurrentSessionSalesTotal.and.returnValue(of(12000));
    cashSessionServiceSpy.getHistory.and.returnValue(of([mockSession]));

    await TestBed.configureTestingModule({
      declarations: [ShiftSessionComponent],
      imports: [FormsModule],
      providers: [
        { provide: CashSessionService, useValue: cashSessionServiceSpy }
      ]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(ShiftSessionComponent);
    component = fixture.componentInstance;
  });

  it('should create component and load session data', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
    expect(cashSessionServiceSpy.getCurrentSession).toHaveBeenCalled();
    expect(cashSessionServiceSpy.getCurrentSessionSalesTotal).toHaveBeenCalled();
    expect(component.currentSession).toEqual(mockSession);
    expect(component.currentSalesTotal).toBe(12000);
    expect(component.expectedClosingBalance).toBe(17000);
  });

  it('should handle load error', () => {
    cashSessionServiceSpy.getCurrentSession.and.returnValue(throwError(() => new Error('Error')));
    fixture.detectChanges();
    expect(component.isLoading).toBeFalse();
    expect(component.loadError).toBe('Failed to load the current session. Please check the server connection.');
  });

  it('should open new session', () => {
    cashSessionServiceSpy.openSession.and.returnValue(of(1));
    component.openingBalance = 5000;

    component.openSession();

    expect(cashSessionServiceSpy.openSession).toHaveBeenCalledWith(5000);
    expect(component.openingBalance).toBe(0);
  });

  it('should close current session', () => {
    cashSessionServiceSpy.closeSession.and.returnValue(of(void 0));
    component.actualCash = 17000;
    component.closeNotes = 'All matched';

    component.closeSession();

    expect(cashSessionServiceSpy.closeSession).toHaveBeenCalledWith(17000, 'All matched');
    expect(component.actualCash).toBe(0);
  });

  it('should track session by id', () => {
    expect(component.trackBySessionId(0, mockSession)).toBe(1);
  });
});
