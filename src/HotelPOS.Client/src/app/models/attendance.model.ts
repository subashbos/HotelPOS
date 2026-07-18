export const ATTENDANCE_STATUSES = ['Present', 'Absent', 'HalfDay', 'OnLeave', 'Holiday', 'WeekOff'] as const;

export interface Attendance {
  id: number;
  employeeId: number;
  employeeName?: string;
  date: string;
  checkInTime?: string;
  checkOutTime?: string;
  status: string;
  workedHours: number;
  remarks?: string;
}

export interface MarkAttendanceRequest {
  id: number;
  employeeId: number;
  date: string;
  checkInTime?: string;
  checkOutTime?: string;
  status: string;
  remarks?: string;
}
