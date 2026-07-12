using System.ComponentModel.DataAnnotations.Schema;

namespace HotelPOS.Domain.Entities
{
    public class SalaryStructure
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public Employee? Employee { get; set; }

        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public decimal Basic { get; set; }

        public decimal Hra { get; set; }

        /// <summary>Dearness Allowance — combined with Basic for PF-wage computation.</summary>
        public decimal Da { get; set; }

        public decimal ConveyanceAllowance { get; set; }

        public decimal MedicalAllowance { get; set; }

        public decimal SpecialAllowance { get; set; }

        public bool PfApplicable { get; set; } = true;

        public bool EsiApplicable { get; set; }

        public bool ProfessionalTaxApplicable { get; set; } = true;

        [NotMapped]
        public decimal GrossMonthly => Basic + Hra + Da + ConveyanceAllowance + MedicalAllowance + SpecialAllowance;
    }
}
