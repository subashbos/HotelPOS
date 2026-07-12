namespace HotelPOS.Application.DTOs.Payroll
{
    public class SalaryStructureDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public decimal Basic { get; set; }
        public decimal Hra { get; set; }
        public decimal Da { get; set; }
        public decimal ConveyanceAllowance { get; set; }
        public decimal MedicalAllowance { get; set; }
        public decimal SpecialAllowance { get; set; }
        public decimal GrossMonthly { get; set; }
        public bool PfApplicable { get; set; }
        public bool EsiApplicable { get; set; }
        public bool ProfessionalTaxApplicable { get; set; }
    }

    /// <summary>DTO used to create or revise an employee's salary structure.</summary>
    public class SaveSalaryStructureDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public decimal Basic { get; set; }
        public decimal Hra { get; set; }
        public decimal Da { get; set; }
        public decimal ConveyanceAllowance { get; set; }
        public decimal MedicalAllowance { get; set; }
        public decimal SpecialAllowance { get; set; }
        public bool PfApplicable { get; set; } = true;
        public bool EsiApplicable { get; set; }
        public bool ProfessionalTaxApplicable { get; set; } = true;
    }

    public class RunPayrollDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
    }

    public class PayslipDto
    {
        public int Id { get; set; }
        public int PayrollRunId { get; set; }
        public int EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public decimal GrossEarnings { get; set; }
        public decimal WorkingDays { get; set; }
        public decimal PaidDays { get; set; }
        public decimal LopDays { get; set; }
        public decimal LopAmount { get; set; }
        public decimal PfEmployee { get; set; }
        public decimal PfEmployer { get; set; }
        public decimal EsiEmployee { get; set; }
        public decimal EsiEmployer { get; set; }
        public decimal ProfessionalTax { get; set; }
        public decimal Tds { get; set; }
        public decimal NetPay { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
    }

    public class PayrollRunDto
    {
        public int Id { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? ProcessedOn { get; set; }
        public List<PayslipDto> Payslips { get; set; } = new();
    }
}
