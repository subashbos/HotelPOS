namespace HotelPOS.Application.DTOs.Attendance
{
    public class AttendanceDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan? CheckInTime { get; set; }
        public TimeSpan? CheckOutTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal WorkedHours { get; set; }
        public string? Remarks { get; set; }
    }

    /// <summary>DTO used to mark or update an employee's attendance for a given date.</summary>
    public class MarkAttendanceDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan? CheckInTime { get; set; }
        public TimeSpan? CheckOutTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Remarks { get; set; }
    }
}
