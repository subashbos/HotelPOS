using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HotelPOS.Domain.Common.Constants;

namespace HotelPOS.Domain.Entities
{
    public class Attendance
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public Employee? Employee { get; set; }

        public DateTime Date { get; set; }

        public TimeSpan? CheckInTime { get; set; }

        public TimeSpan? CheckOutTime { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = AttendanceStatuses.Present;

        public decimal WorkedHours { get; set; }

        [MaxLength(300)]
        public string? Remarks { get; set; }
    }
}
