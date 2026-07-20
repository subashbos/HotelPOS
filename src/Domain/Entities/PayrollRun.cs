using System.ComponentModel.DataAnnotations;
using HotelPOS.Domain.Common.Constants;

namespace HotelPOS.Domain.Entities
{
    public class PayrollRun
    {
        public int Id { get; set; }

        /// <summary>1-12.</summary>
        public int Month { get; set; }

        public int Year { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = PayrollRunStatuses.Draft;

        public DateTime? ProcessedOn { get; set; }

        public int? ProcessedByUserId { get; set; }

        public DateTime? PaidOn { get; set; }

        public List<Payslip> Payslips { get; set; } = new();
    }
}
