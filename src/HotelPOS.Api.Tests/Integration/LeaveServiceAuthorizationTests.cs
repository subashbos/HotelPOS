using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class LeaveServiceAuthorizationTests
    {
        private readonly Mock<ILeaveRepository> _repo = new();

        private LeaveService BuildService(Mock<IAuthorizationService> auth) =>
            new(_repo.Object, auth.Object);

        [Fact]
        public async Task GetBalancesAsync_WhenUnauthorized_Throws()
        {
            var auth = TestAuthorization.DenyAll();
            var service = BuildService(auth);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetBalancesAsync(1, 2026));
        }

        [Fact]
        public async Task GetBalancesAsync_DoesNotAllowViewingAnotherEmployeesRecordWithoutPermission()
        {
            // Bare authentication with no HrLeave grant must not be enough to view ANY employee's
            // leave balances, including one's own — this module has no self-service path.
            var auth = new Mock<IAuthorizationService>();
            auth.Setup(a => a.EnsurePermission(PermissionModules.HrLeave))
                .Throws(new UnauthorizedAccessException("Access denied."));

            var service = BuildService(auth);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetBalancesAsync(42, 2026));
        }

        [Fact]
        public async Task GetBalancesAsync_WhenGrantedHrLeave_ReturnsAnyEmployeesBalances()
        {
            var auth = TestAuthorization.AllowAll();
            _repo.Setup(r => r.GetLeaveTypesAsync()).ReturnsAsync(new List<LeaveType>());
            _repo.Setup(r => r.GetBalancesAsync(42, 2026)).ReturnsAsync(new List<LeaveBalance>
            {
                new LeaveBalance { Id = 1, EmployeeId = 42 }
            });

            var service = BuildService(auth);
            var result = await service.GetBalancesAsync(42, 2026);

            Assert.Single(result);
            auth.Verify(a => a.EnsurePermission(PermissionModules.HrLeave), Times.Once);
        }

        [Fact]
        public async Task GetRequestsAsync_WhenUnauthorized_Throws()
        {
            var auth = TestAuthorization.DenyAll();
            var service = BuildService(auth);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetRequestsAsync());
        }

        [Fact]
        public async Task GetRequestsAsync_WithNoEmployeeIdFilter_StillRequiresPermission()
        {
            // Omitting employeeId returns every employee's leave requests, so this must be
            // gated too, not just the single-employee lookup.
            var auth = new Mock<IAuthorizationService>();
            auth.Setup(a => a.EnsurePermission(PermissionModules.HrLeave))
                .Throws(new UnauthorizedAccessException("Access denied."));

            var service = BuildService(auth);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetRequestsAsync(null, null));
        }

        [Fact]
        public async Task GetRequestsAsync_WhenGrantedHrLeave_ReturnsAnyEmployeesRequests()
        {
            var auth = TestAuthorization.AllowAll();
            _repo.Setup(r => r.GetRequestsAsync(42, null)).ReturnsAsync(new List<LeaveRequest>
            {
                new LeaveRequest { Id = 1, EmployeeId = 42 }
            });

            var service = BuildService(auth);
            var result = await service.GetRequestsAsync(42);

            Assert.Single(result);
            auth.Verify(a => a.EnsurePermission(PermissionModules.HrLeave), Times.Once);
        }
    }
}
