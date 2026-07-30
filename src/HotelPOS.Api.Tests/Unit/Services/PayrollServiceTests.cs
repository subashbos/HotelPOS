using HotelPOS.Application.Common.Models;
using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases;
using HotelPOS.Domain.Entities;
using HotelPOS.Domain.Events;
using HotelPOS.Domain.Common.Constants;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HotelPOS.Tests.Unit.Services
{
    public class PayrollServiceTests
    {
        private readonly Mock<IPayrollRepository> _payrollRepoMock = new();
        private readonly Mock<IEmployeeRepository> _employeeRepoMock = new();
        private readonly Mock<IAttendanceRepository> _attendanceRepoMock = new();
        private readonly Mock<IAuthorizationService> _authorizationMock = TestAuthorization.AllowAll();
        private readonly Mock<IMediator> _mediatorMock = new();
        private readonly PayrollService _service;

        public PayrollServiceTests()
        {
            _service = new PayrollService(
                _payrollRepoMock.Object,
                _employeeRepoMock.Object,
                _attendanceRepoMock.Object,
                _authorizationMock.Object,
                mediator: _mediatorMock.Object);
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
        public void CalculatePayslip_UsesSuppliedProfessionalTaxThresholdAndAmount_InsteadOfStatutoryDefault()
        {
            // Professional Tax is a state subject — an admin who has configured their state's own
            // threshold/amount via SystemSetting must see those values applied, not the Karnataka-shaped
            // 15000/200 statutory default baked into IndianStatutoryDefaults.
            var structure = new SalaryStructure { Basic = 12000, ProfessionalTaxApplicable = true };

            var withDefaults = _service.CalculatePayslip(structure, 30, 30);
            var withStateOverride = _service.CalculatePayslip(
                structure, 30, 30, tdsRuleSet: null,
                professionalTaxThreshold: 10000m,
                professionalTaxAmount: 150m);

            Assert.Equal(0, withDefaults.ProfessionalTax); // below the 15000 statutory default
            Assert.Equal(150m, withStateOverride.ProfessionalTax); // above the 10000 override
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
        public void CalculatePayslip_NoTdsRuleSetProvided_TdsIsZero()
        {
            var structure = new SalaryStructure { Basic = 100000, Hra = 40000 };

            var payslip = _service.CalculatePayslip(structure, 30, 30);

            Assert.Equal(0, payslip.Tds);
        }

        [Fact]
        public void CalculatePayslip_WithTdsRuleSet_ComputesNonZeroTds()
        {
            var structure = new SalaryStructure { Basic = 150000, Hra = 60000 };
            var ruleSet = new TdsRuleSet
            {
                Config = new TdsConfig { FinancialYearStart = 2025, StandardDeduction = 75000m, RebateIncomeLimit = 1200000m, CessRatePercent = 4m },
                Slabs = new List<TdsSlab>
                {
                    new TdsSlab { IncomeFrom = 0m, IncomeTo = 400000m, RatePercent = 0m, DisplayOrder = 1 },
                    new TdsSlab { IncomeFrom = 400000m, IncomeTo = 800000m, RatePercent = 5m, DisplayOrder = 2 },
                    new TdsSlab { IncomeFrom = 800000m, IncomeTo = 1200000m, RatePercent = 10m, DisplayOrder = 3 },
                    new TdsSlab { IncomeFrom = 1200000m, IncomeTo = 1600000m, RatePercent = 15m, DisplayOrder = 4 },
                    new TdsSlab { IncomeFrom = 1600000m, IncomeTo = 2000000m, RatePercent = 20m, DisplayOrder = 5 },
                    new TdsSlab { IncomeFrom = 2000000m, IncomeTo = 2400000m, RatePercent = 25m, DisplayOrder = 6 },
                    new TdsSlab { IncomeFrom = 2400000m, IncomeTo = null, RatePercent = 30m, DisplayOrder = 7 }
                }
            };

            var payslip = _service.CalculatePayslip(structure, 30, 30, ruleSet);

            // Annual gross 2,520,000 - 75,000 standard deduction = 2,445,000 taxable, above the
            // 1,200,000 rebate limit, so this must be taxed via the slabs (non-zero, non-trivial).
            Assert.True(payslip.Tds > 0);
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

        [Fact]
        public async Task RunPayrollAsync_ValidatesInputs_RunsCorrectly()
        {
            // Invalid month
            await Assert.ThrowsAsync<ArgumentException>(() => _service.RunPayrollAsync(13, 2026, 1));

            // Already processed
            _payrollRepoMock.Setup(r => r.GetRunAsync(5, 2026)).ReturnsAsync(new PayrollRun());
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.RunPayrollAsync(5, 2026, 1));

            // Setup for successful run
            _payrollRepoMock.Setup(r => r.GetRunAsync(6, 2026)).ReturnsAsync((PayrollRun?)null);
            
            var employees = new List<Employee>
            {
                new Employee { Id = 1, FirstName = "A", Status = EmployeeStatuses.Active },
                new Employee { Id = 2, FirstName = "B", Status = EmployeeStatuses.Resigned }
            };
            _employeeRepoMock.Setup(e => e.GetAllAsync()).ReturnsAsync(employees);

            var structure = new SalaryStructure { Id = 10, EmployeeId = 1, Basic = 10000 };
            _payrollRepoMock.Setup(r => r.GetCurrentSalaryStructureAsync(1, It.IsAny<DateTime>())).ReturnsAsync(structure);

            var attendance = new List<Attendance>
            {
                new Attendance { EmployeeId = 1, Status = AttendanceStatuses.Absent },
                new Attendance { EmployeeId = 1, Status = AttendanceStatuses.HalfDay }
            };
            _attendanceRepoMock.Setup(a => a.GetByEmployeeAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(attendance);

            // Act
            var run = await _service.RunPayrollAsync(6, 2026, 1);

            // Assert
            Assert.NotNull(run);
            Assert.Equal(6, run.Month);
            Assert.Equal(2026, run.Year);
            Assert.Single(run.Payslips);
            Assert.Equal(1, run.Payslips[0].EmployeeId);
            
            _payrollRepoMock.Verify(r => r.AddRunAsync(run), Times.Once);
            _mediatorMock.Verify(m => m.Publish(It.Is<EntityActionEvent>(e =>
                e.EntityName == "PayrollRun" && e.Action == "Create"), default), Times.Once);
        }

        [Fact]
        public async Task MarkRunAsPaidAsync_UpdatesStatusAndSaves()
        {
            // Missing run
            _payrollRepoMock.Setup(r => r.GetRunByIdAsync(99)).ReturnsAsync((PayrollRun?)null);
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.MarkRunAsPaidAsync(99));

            // Invalid status
            var invalidRun = new PayrollRun { Id = 1, Status = PayrollRunStatuses.Paid };
            _payrollRepoMock.Setup(r => r.GetRunByIdAsync(1)).ReturnsAsync(invalidRun);
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.MarkRunAsPaidAsync(1));

            // Successful path
            var validRun = new PayrollRun 
            { 
                Id = 2, 
                Status = PayrollRunStatuses.Processed,
                Payslips = new List<Payslip> { new Payslip { Id = 10 } }
            };
            _payrollRepoMock.Setup(r => r.GetRunByIdAsync(2)).ReturnsAsync(validRun);

            // Act
            await _service.MarkRunAsPaidAsync(2);

            // Assert
            Assert.Equal(PayrollRunStatuses.Paid, validRun.Status);
            Assert.NotNull(validRun.PaidOn);
            Assert.Equal(PayslipPaymentStatuses.Paid, validRun.Payslips.First().PaymentStatus);
            _payrollRepoMock.Verify(r => r.UpdateRunAsync(validRun), Times.Once);
            _mediatorMock.Verify(m => m.Publish(It.Is<EntityActionEvent>(e =>
                e.EntityName == "PayrollRun" && e.EntityId == 2 && e.Action == "Update"), default), Times.Once);
        }

        [Fact]
        public async Task RunPayrollAsync_ExistingRunIsVoided_AllowsRerun()
        {
            _payrollRepoMock.Setup(r => r.GetRunAsync(7, 2026))
                .ReturnsAsync(new PayrollRun { Id = 1, Month = 7, Year = 2026, Status = PayrollRunStatuses.Voided });
            _employeeRepoMock.Setup(e => e.GetAllAsync()).ReturnsAsync(new List<Employee>());

            var run = await _service.RunPayrollAsync(7, 2026, 1);

            Assert.NotNull(run);
            Assert.Equal(7, run.Month);
            _payrollRepoMock.Verify(r => r.AddRunAsync(run), Times.Once);
        }

        [Fact]
        public async Task VoidRunAsync_RunNotFound_ThrowsKeyNotFoundException()
        {
            _payrollRepoMock.Setup(r => r.GetRunByIdAsync(99)).ReturnsAsync((PayrollRun?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.VoidRunAsync(99, "Wrong month"));
        }

        [Fact]
        public async Task VoidRunAsync_AlreadyVoided_ThrowsInvalidOperationException()
        {
            var run = new PayrollRun { Id = 1, Status = PayrollRunStatuses.Voided };
            _payrollRepoMock.Setup(r => r.GetRunByIdAsync(1)).ReturnsAsync(run);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.VoidRunAsync(1, "Duplicate"));
        }

        [Fact]
        public async Task VoidRunAsync_AlreadyPaid_ThrowsInvalidOperationException()
        {
            var run = new PayrollRun { Id = 1, Status = PayrollRunStatuses.Paid };
            _payrollRepoMock.Setup(r => r.GetRunByIdAsync(1)).ReturnsAsync(run);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.VoidRunAsync(1, "Too late"));
        }

        [Fact]
        public async Task VoidRunAsync_Processed_SetsVoidedStatusAndReasonAndPublishesEvent()
        {
            var run = new PayrollRun { Id = 3, Month = 6, Year = 2026, Status = PayrollRunStatuses.Processed };
            _payrollRepoMock.Setup(r => r.GetRunByIdAsync(3)).ReturnsAsync(run);

            await _service.VoidRunAsync(3, "Attendance data was wrong");

            Assert.Equal(PayrollRunStatuses.Voided, run.Status);
            Assert.Equal("Attendance data was wrong", run.VoidReason);
            _payrollRepoMock.Verify(r => r.UpdateRunAsync(run), Times.Once);
            _mediatorMock.Verify(m => m.Publish(It.Is<EntityActionEvent>(e =>
                e.EntityName == "PayrollRun" && e.EntityId == 3 && e.Action == "Void"), default), Times.Once);
        }

        [Fact]
        public async Task SaveSalaryStructureAsync_ValidatesAndSaves()
        {
            // Null check
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.SaveSalaryStructureAsync(null!));

            // Invalid validation
            var invalidStructure = new SalaryStructure { EmployeeId = 0, Basic = -100 };
            await Assert.ThrowsAsync<ArgumentException>(() => _service.SaveSalaryStructureAsync(invalidStructure));

            // Add new structure
            var newStructure = new SalaryStructure { Id = 0, EmployeeId = 1, Basic = 15000 };
            await _service.SaveSalaryStructureAsync(newStructure);
            _payrollRepoMock.Verify(r => r.AddSalaryStructureAsync(newStructure), Times.Once);
            _mediatorMock.Verify(m => m.Publish(It.Is<EntityActionEvent>(e =>
                e.EntityName == "SalaryStructure" && e.Action == "Create"), default), Times.Once);

            // Update existing structure
            var existingStructure = new SalaryStructure { Id = 5, EmployeeId = 1, Basic = 15000 };
            await _service.SaveSalaryStructureAsync(existingStructure);
            _payrollRepoMock.Verify(r => r.UpdateSalaryStructureAsync(existingStructure), Times.Once);
            _mediatorMock.Verify(m => m.Publish(It.Is<EntityActionEvent>(e =>
                e.EntityName == "SalaryStructure" && e.EntityId == 5 && e.Action == "Update"), default), Times.Once);
        }

        [Fact]
        public async Task OtherAsyncServiceMethods_CallRepositoryDirectly()
        {
            _payrollRepoMock.Setup(r => r.GetSalaryStructuresAsync(1)).ReturnsAsync(new List<SalaryStructure>());
            var structs = await _service.GetSalaryStructuresAsync(1);
            Assert.NotNull(structs);

            _payrollRepoMock.Setup(r => r.GetRunsAsync()).ReturnsAsync(new List<PayrollRun>());
            var runs = await _service.GetRunsAsync();
            Assert.NotNull(runs);

            var run = new PayrollRun { Id = 3 };
            _payrollRepoMock.Setup(r => r.GetRunByIdAsync(3)).ReturnsAsync(run);
            var resultRun = await _service.GetRunByIdAsync(3);
            Assert.Equal(run, resultRun);

            _payrollRepoMock.Setup(r => r.GetPayslipsByEmployeeAsync(1)).ReturnsAsync(new List<Payslip>());
            var payslips = await _service.GetPayslipsByEmployeeAsync(1);
            Assert.NotNull(payslips);
        }
    }
}
