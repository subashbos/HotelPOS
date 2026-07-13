namespace HotelPOS.Application.DTOs.Leave
{
    public class LeaveTypeDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal AnnualQuota { get; set; }
        public bool IsPaid { get; set; }
        public bool CarryForwardAllowed { get; set; }
    }

    public class LeaveBalanceDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public int LeaveTypeId { get; set; }
        public string? LeaveTypeName { get; set; }
        public int Year { get; set; }
        public decimal EntitledDays { get; set; }
        public decimal UsedDays { get; set; }
        public decimal AvailableDays { get; set; }
    }

    public class LeaveRequestDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public int LeaveTypeId { get; set; }
        public string? LeaveTypeName { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalDays { get; set; }
        public string? Reason { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime AppliedOn { get; set; }
        public int? ApprovedByEmployeeId { get; set; }
        public DateTime? ActionedOn { get; set; }
        public string? RejectionReason { get; set; }
    }

    /// <summary>DTO used to submit a new leave request.</summary>
    public class ApplyLeaveDto
    {
        public int EmployeeId { get; set; }
        public int LeaveTypeId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? Reason { get; set; }
    }

    public class RejectLeaveDto
    {
        public string Reason { get; set; } = string.Empty;
    }
}
