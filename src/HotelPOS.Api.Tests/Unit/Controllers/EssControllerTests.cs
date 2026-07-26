using AutoMapper;
using HotelPOS.Api.Controllers;
using HotelPOS.Application.DTOs.Employee;
using HotelPOS.Application.DTOs.Leave;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.Controllers
{
    /// <summary>
    /// EssController must never trust a client-supplied employee ID — every action resolves the
    /// caller's own Employee record from IUserContext.CurrentUserId first, so these tests focus on
    /// that self-scoping and on the 404 behavior when no employee is linked to the caller's login.
    /// </summary>
    public class EssControllerTests
    {
        private static readonly IMapper Mapper = new MapperConfiguration(
            cfg => cfg.AddProfile(new HotelPOS.Application.Common.Mappings.MappingProfile()),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance).CreateMapper();

        private readonly Mock<IEmployeeRepository> _employeeRepo = new();
        private readonly Mock<ILeaveService> _leaveService = new();
        private readonly Mock<IPayrollService> _payrollService = new();
        private readonly Mock<IUserContext> _userContext = new();

        private EssController BuildController() =>
            new(_employeeRepo.Object, _leaveService.Object, _payrollService.Object, _userContext.Object, Mapper);

        private void AsUser(int userId, int employeeId)
        {
            _userContext.Setup(u => u.CurrentUserId).Returns(userId);
            _employeeRepo.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(new Employee { Id = employeeId, UserId = userId, FirstName = "Self" });
        }

        [Fact]
        public async Task GetProfile_NoLinkedEmployee_ReturnsNotFound()
        {
            _userContext.Setup(u => u.CurrentUserId).Returns(99);
            _employeeRepo.Setup(r => r.GetByUserIdAsync(99)).ReturnsAsync((Employee?)null);
            var controller = BuildController();

            var result = await controller.GetProfile();

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetProfile_NoCurrentUserId_ReturnsNotFound()
        {
            _userContext.Setup(u => u.CurrentUserId).Returns((int?)null);
            var controller = BuildController();

            var result = await controller.GetProfile();

            Assert.IsType<NotFoundObjectResult>(result.Result);
            _employeeRepo.Verify(r => r.GetByUserIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetProfile_ResolvesOwnEmployeeFromUserContext()
        {
            AsUser(userId: 7, employeeId: 42);
            var controller = BuildController();

            var result = await controller.GetProfile();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<EmployeeDto>(ok.Value);
            Assert.Equal(42, dto.Id);
        }

        [Fact]
        public async Task ApplyLeave_IgnoresClientSuppliedEmployeeId_UsesOwnResolvedEmployeeId()
        {
            AsUser(userId: 7, employeeId: 42);
            var controller = BuildController();

            LeaveRequest? captured = null;
            _leaveService.Setup(s => s.ApplyLeaveAsync(It.IsAny<LeaveRequest>()))
                .Callback<LeaveRequest>(r => captured = r)
                .Returns(Task.CompletedTask);

            var maliciousRequest = new ApplyLeaveDto
            {
                EmployeeId = 999, // attempting to apply leave on behalf of a different employee
                LeaveTypeId = 1,
                FromDate = DateTime.Today,
                ToDate = DateTime.Today
            };

            await controller.ApplyLeave(maliciousRequest);

            Assert.NotNull(captured);
            Assert.Equal(42, captured!.EmployeeId);
        }

        [Fact]
        public async Task GetLeaveBalances_PassesOwnResolvedEmployeeId_NotAnyClientValue()
        {
            AsUser(userId: 7, employeeId: 42);
            _leaveService.Setup(s => s.GetBalancesAsync(42, It.IsAny<int>())).ReturnsAsync(new List<LeaveBalance>());
            var controller = BuildController();

            await controller.GetLeaveBalances(year: 2026);

            _leaveService.Verify(s => s.GetBalancesAsync(42, 2026), Times.Once);
        }

        [Fact]
        public async Task GetPayslips_PassesOwnResolvedEmployeeId()
        {
            AsUser(userId: 7, employeeId: 42);
            _payrollService.Setup(s => s.GetPayslipsByEmployeeAsync(42)).ReturnsAsync(new List<Payslip>());
            var controller = BuildController();

            await controller.GetPayslips();

            _payrollService.Verify(s => s.GetPayslipsByEmployeeAsync(42), Times.Once);
        }

        [Fact]
        public async Task CancelLeave_PassesOwnResolvedEmployeeId()
        {
            AsUser(userId: 7, employeeId: 42);
            var controller = BuildController();

            await controller.CancelLeave(10);

            _leaveService.Verify(s => s.CancelLeaveAsync(10, 42), Times.Once);
        }

        [Fact]
        public async Task CancelLeave_NotOwnRequest_ReturnsForbidden()
        {
            AsUser(userId: 7, employeeId: 42);
            _leaveService.Setup(s => s.CancelLeaveAsync(10, 42))
                .ThrowsAsync(new UnauthorizedAccessException("You can only cancel your own leave requests."));
            var controller = BuildController();

            var result = await controller.CancelLeave(10);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task CancelLeave_RequestNotFound_ReturnsNotFound()
        {
            AsUser(userId: 7, employeeId: 42);
            _leaveService.Setup(s => s.CancelLeaveAsync(10, 42)).ThrowsAsync(new KeyNotFoundException());
            var controller = BuildController();

            var result = await controller.CancelLeave(10);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateProfile_OnlyMutatesAllowedFields()
        {
            var employee = new Employee
            {
                Id = 42,
                UserId = 7,
                FirstName = "Self",
                Status = "Active",
                Pan = "ABCDE1234F",
                BankAccountNumber = "111222333"
            };
            _userContext.Setup(u => u.CurrentUserId).Returns(7);
            _employeeRepo.Setup(r => r.GetByUserIdAsync(7)).ReturnsAsync(employee);
            var controller = BuildController();

            await controller.UpdateProfile(new EssProfileUpdateDto
            {
                Phone = "9999999999",
                Address = "New Address",
                EmergencyContactName = "Jane",
                EmergencyContactPhone = "8888888888"
            });

            Assert.Equal("9999999999", employee.Phone);
            Assert.Equal("New Address", employee.Address);
            // Fields outside the self-service DTO must be untouched.
            Assert.Equal("Active", employee.Status);
            Assert.Equal("ABCDE1234F", employee.Pan);
            Assert.Equal("111222333", employee.BankAccountNumber);
            _employeeRepo.Verify(r => r.UpdateAsync(employee), Times.Once);
        }
    }
}
