import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { AuditComponent } from './audit.component';
import { AuditService } from '../../../services/audit.service';
import { AuditLog } from '../../../models/audit.model';

describe('AuditComponent', () => {
  let component: AuditComponent;
  let fixture: ComponentFixture<AuditComponent>;
  let auditServiceSpy: jasmine.SpyObj<AuditService>;

  beforeEach(async () => {
    auditServiceSpy = jasmine.createSpyObj('AuditService', ['getLogs']);
    auditServiceSpy.getLogs.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      declarations: [AuditComponent],
      imports: [FormsModule],
      providers: [
        { provide: AuditService, useValue: auditServiceSpy }
      ]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(AuditComponent);
    component = fixture.componentInstance;
  });

  it('should create component and load audit logs', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
    expect(auditServiceSpy.getLogs).toHaveBeenCalled();
  });

  it('should handle audit load error', () => {
    auditServiceSpy.getLogs.and.returnValue(throwError(() => new Error('Error')));
    component.load();
    expect(component.loadError).toBe('Failed to load the audit log. Please check the server connection.');
  });

  it('should sort audit logs descending by timestamp', () => {
    const mockLogs: AuditLog[] = [
      { sNo: 1, id: 1, entityName: 'Order', entityId: 10, action: 'Login', username: 'user1', details: '', timestamp: '2026-01-01T10:00:00Z' },
      { sNo: 2, id: 2, entityName: 'Item', entityId: 11, action: 'CreateItem', username: 'user2', details: '', timestamp: '2026-01-02T10:00:00Z' },
      { sNo: 3, id: 3, entityName: 'Item', entityId: 12, action: 'DeleteItem', username: 'user1', details: '', timestamp: '2026-01-01T12:00:00Z' }
    ];
    auditServiceSpy.getLogs.and.returnValue(of(mockLogs));
    component.load();
    expect(component.logs.map(l => l.id)).toEqual([2, 3, 1]);
  });

  it('should return log id in trackByLogId', () => {
    const log: AuditLog = { sNo: 1, id: 42, entityName: 'Order', entityId: 1, action: 'Test', username: 'u', details: '', timestamp: '2026-01-01' };
    expect(component.trackByLogId(0, log)).toBe(42);
  });
});
