using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases;
using HotelPOS.Domain.Entities;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.Services
{
    public class PayrollServiceTests
    {
        private readonly PayrollService _service;

        public PayrollServiceTests()
        {
            _service = new PayrollService(
                Mock.Of<IPayrollRepository>(),
                Mock.Of<IEmployeeRepository>(),
                Mock.Of<IAttendanceRepository>());
        }

        [Fact]
        public void CalculatePayslip_FullAttendance_NoLopAndFullGross()
        {
            var structure = new SalaryStructure
            {
                Basic = 20000,
                Hra = 8000,
                Da = 0,
                ConveyanceAllowance = 1600,
                MedicalAllowance = 1250,
                SpecialAllowance = 0,
                PfApplicable = true,
                EsiApplicable = false,
                ProfessionalTaxApplicable = true
            };

            var payslip = _service.CalculatePayslip(structure, workingDays: 30, paidDays: 30);

            Assert.Equal(0, payslip.LopDays);
            Assert.Equal(structure.GrossMonthly, payslip.GrossEarnings);
        }

        [Fact]
        public void CalculatePayslip_PfCappedAtStatutoryWageCeiling()
        {
            // Basic (20000) exceeds the Rs. 15,000 PF wage ceiling, so PF should be computed on 15000.
            var structure = new SalaryStructure
            {
                Basic = 20000,
                Da = 0,
                PfApplicable = true
            };

            var payslip = _service.CalculatePayslip(structure, workingDays: 30, paidDays: 30);

            Assert.Equal(1800m, payslip.PfEmployee); // 15000 * 12%
            Assert.Equal(1800m, payslip.PfEmployer);
        }

        [Fact]
        public void CalculatePayslip_EsiAppliedOnlyWhenGrossAtOrBelowThreshold()
        {
            var lowWageStructure = new SalaryStructure { Basic = 15000, Hra = 5000, EsiApplicable = true };
            var highWageStructure = new SalaryStructure { Basic = 30000, Hra = 10000, EsiApplicable = true };

            var lowWagePayslip = _service.CalculatePayslip(lowWageStructure, 30, 30);
            var highWagePayslip = _service.CalculatePayslip(highWageStructure, 30, 30);

            Assert.True(lowWagePayslip.EsiEmployee > 0);
            Assert.Equal(0, highWagePayslip.EsiEmployee);
        }

        [Fact]
        public void CalculatePayslip_ProfessionalTaxAppliedAboveThreshold()
        {
            var belowThreshold = new SalaryStructure { Basic = 10000, ProfessionalTaxApplicable = true };
            var aboveThreshold = new SalaryStructure { Basic = 20000, ProfessionalTaxApplicable = true };

            var belowPayslip = _service.CalculatePayslip(belowThreshold, 30, 30);
            var abovePayslip = _service.CalculatePayslip(aboveThreshold, 30, 30);

            Assert.Equal(0, belowPayslip.ProfessionalTax);
            Assert.Equal(200m, abovePayslip.ProfessionalTax);
        }

        [Fact]
        public void CalculatePayslip_PartialAttendance_ProratesGrossAndLop()
        {
            var structure = new SalaryStructure { Basic = 15000, Hra = 5000 };

            var payslip = _service.CalculatePayslip(structure, workingDays: 30, paidDays: 27);

            Assert.Equal(3, payslip.LopDays);
            Assert.Equal(Math.Round(structure.GrossMonthly * 27 / 30, 2), payslip.GrossEarnings);
        }

        [Fact]
        public void CalculatePayslip_TdsIsNeverAutoComputed()
        {
            var structure = new SalaryStructure { Basic = 100000, Hra = 40000 };

            var payslip = _service.CalculatePayslip(structure, 30, 30);

            Assert.Equal(0, payslip.Tds);
        }

        [Fact]
        public void CalculatePayslip_NetPayIsGrossMinusAllDeductions()
        {
            var structure = new SalaryStructure
            {
                Basic = 20000,
                Hra = 8000,
                PfApplicable = true,
                EsiApplicable = false,
                ProfessionalTaxApplicable = true
            };

            var payslip = _service.CalculatePayslip(structure, 30, 30);

            var expectedNet = payslip.GrossEarnings - payslip.PfEmployee - payslip.EsiEmployee - payslip.ProfessionalTax - payslip.Tds;
            Assert.Equal(expectedNet, payslip.NetPay);
        }
    }
}
