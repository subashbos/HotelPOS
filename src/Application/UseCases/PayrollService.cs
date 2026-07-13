using FluentValidation;
using HotelPOS.Application.Common.Validators;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.UseCases
{
    /// <summary>
    /// Computes monthly payroll using Indian statutory parameters (EPF, ESI) with Professional
    /// Tax and TDS handled as documented approximations — see <see cref="IndianStatutoryDefaults"/>.
    /// </summary>
    public class PayrollService : IPayrollService
    {
        private readonly IPayrollRepository _payrollRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly IValidator<SalaryStructure> _validator;

        public PayrollService(
            IPayrollRepository payrollRepository,
            IEmployeeRepository employeeRepository,
            IAttendanceRepository attendanceRepository,
            IValidator<SalaryStructure>? validator = null)
        {
            _payrollRepository = payrollRepository;
            _employeeRepository = employeeRepository;
            _attendanceRepository = attendanceRepository;
            _validator = validator ?? new SalaryStructureValidator();
        }

        public async Task<List<SalaryStructure>> GetSalaryStructuresAsync(int employeeId)
        {
            return await _payrollRepository.GetSalaryStructuresAsync(employeeId);
        }

        public async Task SaveSalaryStructureAsync(SalaryStructure structure)
        {
            if (structure == null) throw new ArgumentNullException(nameof(structure));

            var result = _validator.Validate(structure);
            if (!result.IsValid)
                throw new ArgumentException(result.Errors[0].ErrorMessage);

            if (structure.Id == 0)
                await _payrollRepository.AddSalaryStructureAsync(structure);
            else
                await _payrollRepository.UpdateSalaryStructureAsync(structure);
        }

        public async Task<PayrollRun> RunPayrollAsync(int month, int year, int? processedByUserId)
        {
            if (month < 1 || month > 12) throw new ArgumentException("Month must be between 1 and 12.");

            var existingRun = await _payrollRepository.GetRunAsync(month, year);
            if (existingRun != null)
                throw new InvalidOperationException($"Payroll for {month:D2}/{year} has already been run.");

            var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var workingDays = DateTime.DaysInMonth(year, month);

            var employees = await _employeeRepository.GetAllAsync();
            var run = new PayrollRun
            {
                Month = month,
                Year = year,
                Status = PayrollRunStatuses.Processed,
                ProcessedOn = DateTime.UtcNow,
                ProcessedByUserId = processedByUserId
            };

            foreach (var employee in employees.Where(e => e.Status == EmployeeStatuses.Active))
            {
                var structure = await _payrollRepository.GetCurrentSalaryStructureAsync(employee.Id, monthEnd);
                if (structure == null) continue; // no salary structure on file yet — skip

                var attendance = await _attendanceRepository.GetByEmployeeAsync(employee.Id, monthStart, monthEnd);
                var absentDays = attendance.Count(a => a.Status == AttendanceStatuses.Absent);
                var halfDays = attendance.Count(a => a.Status == AttendanceStatuses.HalfDay);
                var lopDays = absentDays + (halfDays * 0.5m);
                var paidDays = Math.Max(0, workingDays - lopDays);

                var payslip = CalculatePayslip(structure, workingDays, paidDays);
                payslip.PayrollRunId = run.Id;
                payslip.EmployeeId = employee.Id;
                payslip.Employee = employee;
                run.Payslips.Add(payslip);
            }

            await _payrollRepository.AddRunAsync(run);
            return run;
        }

        public async Task MarkRunAsPaidAsync(int runId)
        {
            var run = await _payrollRepository.GetRunByIdAsync(runId)
                ?? throw new KeyNotFoundException($"Payroll run #{runId} not found.");

            if (run.Status != PayrollRunStatuses.Processed)
                throw new InvalidOperationException("Only a processed payroll run can be marked as paid.");

            var paidOn = DateTime.UtcNow;
            run.Status = PayrollRunStatuses.Paid;
            run.PaidOn = paidOn;
            foreach (var payslip in run.Payslips)
            {
                payslip.PaymentStatus = PayslipPaymentStatuses.Paid;
                payslip.PaidOn = paidOn;
            }

            await _payrollRepository.UpdateRunAsync(run);
        }

        public async Task<List<PayrollRun>> GetRunsAsync()
        {
            return await _payrollRepository.GetRunsAsync();
        }

        public async Task<PayrollRun?> GetRunByIdAsync(int id)
        {
            return await _payrollRepository.GetRunByIdAsync(id);
        }

        public async Task<List<Payslip>> GetPayslipsByEmployeeAsync(int employeeId)
        {
            return await _payrollRepository.GetPayslipsByEmployeeAsync(employeeId);
        }

        public Payslip CalculatePayslip(SalaryStructure structure, decimal workingDays, decimal paidDays)
        {
            if (structure == null) throw new ArgumentNullException(nameof(structure));
            if (workingDays <= 0) throw new ArgumentException("Working days must be greater than zero.");

            var proration = Math.Clamp(paidDays / workingDays, 0m, 1m);
            var grossMonthly = structure.GrossMonthly;
            var grossEarnings = Math.Round(grossMonthly * proration, 2);
            var lopDays = Math.Max(0, workingDays - paidDays);
            var lopAmount = Math.Round(grossMonthly - grossEarnings, 2);

            decimal pfEmployee = 0, pfEmployer = 0;
            if (structure.PfApplicable)
            {
                var pfWageMonthly = Math.Min(structure.Basic + structure.Da, IndianStatutoryDefaults.PfWageCeiling);
                var pfWage = Math.Round(pfWageMonthly * proration, 2);
                pfEmployee = Math.Round(pfWage * IndianStatutoryDefaults.PfEmployeeRate, 2);
                pfEmployer = Math.Round(pfWage * IndianStatutoryDefaults.PfEmployerRate, 2);
            }

            decimal esiEmployee = 0, esiEmployer = 0;
            if (structure.EsiApplicable && grossMonthly <= IndianStatutoryDefaults.EsiWageThreshold)
            {
                esiEmployee = Math.Round(grossEarnings * IndianStatutoryDefaults.EsiEmployeeRate, 2);
                esiEmployer = Math.Round(grossEarnings * IndianStatutoryDefaults.EsiEmployerRate, 2);
            }

            decimal professionalTax = 0;
            if (structure.ProfessionalTaxApplicable && grossEarnings > IndianStatutoryDefaults.ProfessionalTaxThreshold)
            {
                professionalTax = IndianStatutoryDefaults.ProfessionalTaxAmount;
            }

            const decimal tds = 0; // manual/statutory override — not auto-computed

            var netPay = Math.Round(grossEarnings - pfEmployee - esiEmployee - professionalTax - tds, 2);

            return new Payslip
            {
                GrossEarnings = grossEarnings,
                WorkingDays = workingDays,
                PaidDays = paidDays,
                LopDays = lopDays,
                LopAmount = lopAmount,
                PfEmployee = pfEmployee,
                PfEmployer = pfEmployer,
                EsiEmployee = esiEmployee,
                EsiEmployer = esiEmployer,
                ProfessionalTax = professionalTax,
                Tds = tds,
                NetPay = netPay,
                PaymentStatus = PayslipPaymentStatuses.Pending
            };
        }
    }
}
