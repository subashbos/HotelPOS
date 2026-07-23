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

        private PayrollService BuildService(Mock<IAuthorizationService> auth) =>
            new(_payrollRepo.Object, _employeeRepo.Object, _attendanceRepo.Object, auth.Object);

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
            auth.Verify(a => a.EnsurePermission(PermissionModules.HrPayroll), Times.Once);
        }
    }
}
