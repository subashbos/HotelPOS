using System.ComponentModel.DataAnnotations;

namespace HotelPOS.Domain.Entities
{
    public class LeaveType
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(10)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Default yearly entitlement in days, credited to a new LeaveBalance row per employee per year.</summary>
        public decimal AnnualQuota { get; set; }

        public bool IsPaid { get; set; } = true;

        public bool CarryForwardAllowed { get; set; }
    }
}
