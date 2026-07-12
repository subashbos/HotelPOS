using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HotelPOS.Domain.Common.Constants;

namespace HotelPOS.Domain.Entities
{
    public class LeaveRequest
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public Employee? Employee { get; set; }

        public int LeaveTypeId { get; set; }

        [ForeignKey(nameof(LeaveTypeId))]
        public LeaveType? LeaveType { get; set; }

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public decimal TotalDays { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = LeaveRequestStatuses.Pending;

        public DateTime AppliedOn { get; set; } = DateTime.UtcNow;

        public int? ApprovedByEmployeeId { get; set; }

        [ForeignKey(nameof(ApprovedByEmployeeId))]
        public Employee? ApprovedByEmployee { get; set; }

        public DateTime? ActionedOn { get; set; }

        [MaxLength(500)]
        public string? RejectionReason { get; set; }
    }
}
