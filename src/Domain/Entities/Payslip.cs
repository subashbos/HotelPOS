using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HotelPOS.Domain.Common.Constants;

namespace HotelPOS.Domain.Entities
{
    public class Payslip
    {
        public int Id { get; set; }

        public int PayrollRunId { get; set; }

        [ForeignKey(nameof(PayrollRunId))]
        public PayrollRun? PayrollRun { get; set; }

        public int EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public Employee? Employee { get; set; }

        public decimal GrossEarnings { get; set; }

        public decimal WorkingDays { get; set; }

        public decimal PaidDays { get; set; }

        /// <summary>Loss-of-pay days beyond available paid leave / attendance.</summary>
        public decimal LopDays { get; set; }

        public decimal LopAmount { get; set; }

        public decimal PfEmployee { get; set; }

        public decimal PfEmployer { get; set; }

        public decimal EsiEmployee { get; set; }

        public decimal EsiEmployer { get; set; }

        public decimal ProfessionalTax { get; set; }

        /// <summary>Manual/statutory override — income-tax TDS is not auto-computed. Defaults to 0.</summary>
        public decimal Tds { get; set; }

        public decimal NetPay { get; set; }

        [MaxLength(20)]
        public string PaymentStatus { get; set; } = PayslipPaymentStatuses.Pending;

        public DateTime? PaidOn { get; set; }
    }
}
