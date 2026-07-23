import { TestBed, ComponentFixture } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { AttendanceComponent } from './attendance.component';
import { AttendanceService } from '../../../services/attendance.service';
import { EmployeeService } from '../../../services/employee.service';
import { Attendance } from '../../../models/attendance.model';

describe('AttendanceComponent', () => {
  let component: AttendanceComponent;
  let fixture: ComponentFixture<AttendanceComponent>;
  let attendanceServiceSpy: jasmine.SpyObj<AttendanceService>;
  let employeeServiceSpy: jasmine.SpyObj<EmployeeService>;

  const mockRecord: Attendance = {
    id: 1,
    employeeId: 1,
    date: '2026-01-22',
    status: 'Present',
    workedHours: 8,
    checkInTime: '09:00:00'
  };

  beforeEach(async () => {
    attendanceServiceSpy = jasmine.createSpyObj('AttendanceService', ['getAttendance', 'markAttendance', 'deleteAttendance']);
    employeeServiceSpy = jasmine.createSpyObj('EmployeeService', ['getEmployees']);

    attendanceServiceSpy.getAttendance.and.returnValue(of([mockRecord]));
    employeeServiceSpy.getEmployees.and.returnValue(of([{ id: 1, firstName: 'John', lastName: 'Doe' } as any]));

    await TestBed.configureTestingModule({
      declarations: [AttendanceComponent],
      imports: [FormsModule],
      providers: [
        { provide: AttendanceService, useValue: attendanceServiceSpy },
        { provide: EmployeeService, useValue: employeeServiceSpy }
      ]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(AttendanceComponent);
    component = fixture.componentInstance;
  });

  it('should create component and load attendance', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
    expect(employeeServiceSpy.getEmployees).toHaveBeenCalled();
    expect(attendanceServiceSpy.getAttendance).toHaveBeenCalledWith(1, component.fromDate, component.toDate);
    expect().toHaveSize();
  });

  it('should open mark form and close form', () => {
    component.openMarkForm();
    expect(component.showForm).toBeTrue();

    component.closeForm();
    expect(component.showForm).toBeFalse();
  });

  it('should mark attendance successfully', () => {
    attendanceServiceSpy.markAttendance.and.returnValue(of(mockRecord));
    fixture.detectChanges();
    component.openMarkForm();
    component.formCheckIn = '09:00';
    component.formCheckOut = '17:00';

    component.markAttendance();

    expect(attendanceServiceSpy.markAttendance).toHaveBeenCalled();
    expect(component.showForm).toBeFalse();
  });

  it('should delete attendance record when confirmed', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    attendanceServiceSpy.deleteAttendance.and.returnValue(of(void 0));
    fixture.detectChanges();

    component.deleteRecord(mockRecord);

    expect(attendanceServiceSpy.deleteAttendance).toHaveBeenCalledWith(1);
  });

  it('should format time correctly', () => {
    expect(component.formatTime('09:30:00')).toBe('09:30');
    expect(component.formatTime(undefined)).toBe('—');
  });

  it('should track record by id', () => {
    expect(component.trackByAttendanceId(0, mockRecord)).toBe(1);
  });
});
