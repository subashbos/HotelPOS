using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class PayrollServiceAuthorizationTests
    {
        private readonly Mock<IPayrollRepository> _payrollRepo = new();
        private readonly Mock<IEmployeeRepository> _employeeRepo = new();
        private readonly Mock<IAttendanceRepository> _attendanceRepo = new();

        private PayrollService BuildService(Mock<IAuthorizationService> auth, ISettingService? settingService = null) =>
            new(_payrollRepo.Object, _employeeRepo.Object, _attendanceRepo.Object, auth.Object,
                validator: null, mediator: null, settingService: settingService);

        [Fact]
        public async Task GetSalaryStructuresAsync_WhenUnauthorized_Throws()
        {
            var auth = TestAuthorization.DenyAll();
            var service = BuildService(auth);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetSalaryStructuresAsync(1));
        }

        [Fact]
        public async Task GetSalaryStructuresAsync_DoesNotAllowViewingAnotherEmployeesRecordWithoutPermission()
        {
            // Bare authentication with no HrPayroll grant must not be enough to view ANY employee's
            // salary structure, including one's own — this module has no self-service path.
            var auth = new Mock<IAuthorizationService>();
            auth.Setup(a => a.EnsurePermission(PermissionModules.HrPayroll))
                .Throws(new UnauthorizedAccessException("Access denied."));

            var service = BuildService(auth);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetSalaryStructuresAsync(42));
        }

        [Fact]
        public async Task GetSalaryStructuresAsync_WhenGrantedHrPayroll_ReturnsAnyEmployeesStructures()
        {
            var auth = TestAuthorization.AllowAll();
            _payrollRepo.Setup(r => r.GetSalaryStructuresAsync(42)).ReturnsAsync(new List<SalaryStructure>
            {
                new SalaryStructure { Id = 1, EmployeeId = 42 }
            });

            var service = BuildService(auth);
            var result = await service.GetSalaryStructuresAsync(42);

            Assert.Single(result);
            auth.Verify(a => a.EnsurePermission(PermissionModules.HrPayroll), Times.Once);
        }

        [Fact]
        public async Task GetPayslipsByEmployeeAsync_WhenUnauthorized_Throws()
        {
            var auth = TestAuthorization.DenyAll();
            var service = BuildService(auth);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetPayslipsByEmployeeAsync(1));
        }

        [Fact]
        public async Task GetPayslipsByEmployeeAsync_WhenGrantedHrPayroll_ReturnsAnyEmployeesPayslips()
        {
            var auth = TestAuthorization.AllowAll();
            _payrollRepo.Setup(r => r.GetPayslipsByEmployeeAsync(42)).ReturnsAsync(new List<Payslip>
            {
                new Payslip { Id = 1, EmployeeId = 42 }
            });

            var service = BuildService(auth);
            var result = await service.GetPayslipsByEmployeeAsync(42);

            Assert.Single(result);
            auth.Verify(a => a.EnsureSelfOrPermission(It.IsAny<int>(), PermissionModules.HrPayroll), Times.Once);
        }

        [Fact]
        public async Task GetPayslipsByEmployeeAsync_ResolvesTargetEmployeesLinkedUserId_ForSelfOrPermissionCheck()
        {
            // The Employee Self-Service payslip view calls this with the caller's own employeeId,
            // so it must resolve that employee's linked UserId and defer the self-vs-permission
            // decision to IAuthorizationService rather than trusting the raw employeeId.
            var auth = new Mock<IAuthorizationService>();
            _employeeRepo.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(new Employee { Id = 42, UserId = 7 });
            _payrollRepo.Setup(r => r.GetPayslipsByEmployeeAsync(42)).ReturnsAsync(new List<Payslip>());

            var service = BuildService(auth);
            await service.GetPayslipsByEmployeeAsync(42);

            auth.Verify(a => a.EnsureSelfOrPermission(7, PermissionModules.HrPayroll), Times.Once);
        }

        [Fact]
        public async Task GetPayslipsByEmployeeAsync_EmployeeWithNoLinkedUserAccount_FallsBackToPermissionCheckOnly()
        {
            var auth = new Mock<IAuthorizationService>();
            _employeeRepo.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(new Employee { Id = 42, UserId = null });
            _payrollRepo.Setup(r => r.GetPayslipsByEmployeeAsync(42)).ReturnsAsync(new List<Payslip>());

            var service = BuildService(auth);
            await service.GetPayslipsByEmployeeAsync(42);

            auth.Verify(a => a.EnsureSelfOrPermission(-1, PermissionModules.HrPayroll), Times.Once);
        }

        [Fact]
        public async Task GetPayslipsByEmployeeAsync_WhenNeitherSelfNorPermitted_Throws()
        {
            var auth = new Mock<IAuthorizationService>();
            _employeeRepo.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(new Employee { Id = 42, UserId = 7 });
            auth.Setup(a => a.EnsureSelfOrPermission(7, PermissionModules.HrPayroll))
                .Throws(new UnauthorizedAccessException("Access denied."));

            var service = BuildService(auth);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetPayslipsByEmployeeAsync(42));
        }

        // ---------- HrPayroll: GetRunsAsync / GetRunByIdAsync ----------

        [Fact]
        public async Task GetRunsAsync_WhenUnauthorized_Throws()
        {
            var auth = TestAuthorization.DenyAll();
            var service = BuildService(auth);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetRunsAsync());

            _payrollRepo.Verify(r => r.GetRunsAsync(), Times.Never);
        }

        [Fact]
        public async Task GetRunsAsync_WhenGrantedHrPayroll_ReturnsRuns()
        {
            var auth = TestAuthorization.AllowAll();
            _payrollRepo.Setup(r => r.GetRunsAsync()).ReturnsAsync(new List<PayrollRun>
            {
                new PayrollRun { Id = 1 }
            });

            var service = BuildService(auth);
            var result = await service.GetRunsAsync();

            Assert.Single(result);
            auth.Verify(a => a.EnsurePermission(PermissionModules.HrPayroll), Times.Once);
        }

        [Fact]
        public async Task GetRunByIdAsync_WhenUnauthorized_Throws()
        {
            var auth = TestAuthorization.DenyAll();
            var service = BuildService(auth);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetRunByIdAsync(1));

            _payrollRepo.Verify(r => r.GetRunByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetRunByIdAsync_WhenGrantedHrPayroll_ReturnsRun()
        {
            var auth = TestAuthorization.AllowAll();
            var run = new PayrollRun { Id = 1 };
            _payrollRepo.Setup(r => r.GetRunByIdAsync(1)).ReturnsAsync(run);

            var service = BuildService(auth);
            var result = await service.GetRunByIdAsync(1);

            Assert.Same(run, result);
            auth.Verify(a => a.EnsurePermission(PermissionModules.HrPayroll), Times.Once);
        }

        // ---------- HrPayrollRun: SaveSalaryStructureAsync ----------

        [Fact]
        public async Task SaveSalaryStructureAsync_WhenUnauthorized_ThrowsAndDoesNotPersist()
        {
            var auth = TestAuthorization.DenyAll();
            var service = BuildService(auth);
            var structure = new SalaryStructure { EmployeeId = 1, Basic = 30000 };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SaveSalaryStructureAsync(structure));

            _payrollRepo.Verify(r => r.AddSalaryStructureAsync(It.IsAny<SalaryStructure>()), Times.Never);
        }

        [Fact]
        public async Task SaveSalaryStructureAsync_WhenGrantedHrPayrollRun_Persists()
        {
            var auth = TestAuthorization.AllowAll();
            var service = BuildService(auth);
            var structure = new SalaryStructure { EmployeeId = 1, Basic = 30000 };

            await service.SaveSalaryStructureAsync(structure);

            auth.Verify(a => a.EnsurePermission(PermissionModules.HrPayrollRun), Times.Once);
            _payrollRepo.Verify(r => r.AddSalaryStructureAsync(structure), Times.Once);
        }

        // ---------- HrPayrollRun: RunPayrollAsync ----------

        [Fact]
        public async Task RunPayrollAsync_WhenUnauthorized_ThrowsAndDoesNotCheckForExistingRun()
        {
            var auth = TestAuthorization.DenyAll();
            var service = BuildService(auth);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.RunPayrollAsync(1, 2025, null));

            _payrollRepo.Verify(r => r.GetRunAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task RunPayrollAsync_WhenGrantedHrPayrollRun_Runs()
        {
            var auth = TestAuthorization.AllowAll();
            _payrollRepo.Setup(r => r.GetRunAsync(1, 2025)).ReturnsAsync((PayrollRun?)null);
            _employeeRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Employee>());
            var service = BuildService(auth);

            await service.RunPayrollAsync(1, 2025, null);

            auth.Verify(a => a.EnsurePermission(PermissionModules.HrPayrollRun), Times.Once);
            _payrollRepo.Verify(r => r.AddRunAsync(It.IsAny<PayrollRun>()), Times.Once);
        }

        [Fact]
        public async Task RunPayrollAsync_UsesConfiguredProfessionalTaxFromSystemSettings_NotTheStatutoryDefault()
        {
            // Confirms the Settings > Payroll tab's Professional Tax fields actually reach a real
            // payroll run, end to end, rather than just being stored and ignored.
            var auth = TestAuthorization.AllowAll();
            var employee = new Employee { Id = 1, Status = EmployeeStatuses.Active };
            var structure = new SalaryStructure
            {
                Id = 1,
                EmployeeId = 1,
                Basic = 12000,
                ProfessionalTaxApplicable = true
            };
            var settings = new SystemSetting { ProfessionalTaxThreshold = 10000m, ProfessionalTaxAmount = 150m };

            _payrollRepo.Setup(r => r.GetRunAsync(1, 2025)).ReturnsAsync((PayrollRun?)null);
            _employeeRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Employee> { employee });
            _payrollRepo.Setup(r => r.GetCurrentSalaryStructureAsync(1, It.IsAny<DateTime>())).ReturnsAsync(structure);
            _attendanceRepo.Setup(r => r.GetByEmployeeAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Attendance>());

            var settingService = new Mock<ISettingService>();
            settingService.Setup(s => s.GetSettingsAsync()).ReturnsAsync(settings);

            var service = BuildService(auth, settingService.Object);

            var run = await service.RunPayrollAsync(1, 2025, null);

            var payslip = Assert.Single(run.Payslips);
            Assert.Equal(150m, payslip.ProfessionalTax); // configured amount, not the 200 statutory default
        }

        // ---------- HrPayrollRun: MarkRunAsPaidAsync ----------

        [Fact]
        public async Task MarkRunAsPaidAsync_WhenUnauthorized_ThrowsAndDoesNotLoadRun()
        {
            var auth = TestAuthorization.DenyAll();
            var service = BuildService(auth);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.MarkRunAsPaidAsync(1));

            _payrollRepo.Verify(r => r.GetRunByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task MarkRunAsPaidAsync_WhenGrantedHrPayrollRun_MarksPaid()
        {
            var auth = TestAuthorization.AllowAll();
            var run = new PayrollRun { Id = 1, Status = PayrollRunStatuses.Processed };
            _payrollRepo.Setup(r => r.GetRunByIdAsync(1)).ReturnsAsync(run);
            var service = BuildService(auth);

            await service.MarkRunAsPaidAsync(1);

            auth.Verify(a => a.EnsurePermission(PermissionModules.HrPayrollRun), Times.Once);
            Assert.Equal(PayrollRunStatuses.Paid, run.Status);
        }

        // ---------- HrPayrollRun: VoidRunAsync ----------

        [Fact]
        public async Task VoidRunAsync_WhenUnauthorized_ThrowsAndDoesNotLoadRun()
        {
            var auth = TestAuthorization.DenyAll();
            var service = BuildService(auth);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.VoidRunAsync(1, "Correction"));

            _payrollRepo.Verify(r => r.GetRunByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task VoidRunAsync_WhenGrantedHrPayrollRun_Voids()
        {
            var auth = TestAuthorization.AllowAll();
            var run = new PayrollRun { Id = 1, Status = PayrollRunStatuses.Processed };
            _payrollRepo.Setup(r => r.GetRunByIdAsync(1)).ReturnsAsync(run);
            var service = BuildService(auth);

            await service.VoidRunAsync(1, "Correction");

            auth.Verify(a => a.EnsurePermission(PermissionModules.HrPayrollRun), Times.Once);
            Assert.Equal(PayrollRunStatuses.Voided, run.Status);
        }
    }
}
