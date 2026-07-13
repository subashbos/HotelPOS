using System.ComponentModel.DataAnnotations.Schema;

namespace HotelPOS.Domain.Entities
{
    public class LeaveBalance
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public Employee? Employee { get; set; }

        public int LeaveTypeId { get; set; }

        [ForeignKey(nameof(LeaveTypeId))]
        public LeaveType? LeaveType { get; set; }

        public int Year { get; set; }

        public decimal EntitledDays { get; set; }

        public decimal UsedDays { get; set; }

        [NotMapped]
        public decimal AvailableDays => EntitledDays - UsedDays;
    }
}
