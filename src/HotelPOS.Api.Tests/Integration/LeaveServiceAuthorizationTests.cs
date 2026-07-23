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
        private readonly Mock<IEmployeeRepository> _employeeRepo = new();

        private LeaveService BuildService(Mock<IAuthorizationService> auth) =>
            new(_repo.Object, _employeeRepo.Object, auth.Object);

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

        private static LeaveRequest ValidRequest(int employeeId) => new()
        {
            EmployeeId = employeeId,
            LeaveTypeId = 2,
            FromDate = new DateTime(2026, 1, 5),
            ToDate = new DateTime(2026, 1, 6)
        };

        [Fact]
        public async Task ApplyLeaveAsync_WhenEmployeeNotFound_ThrowsArgumentException()
        {
            var auth = TestAuthorization.AllowAll();
            _employeeRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Employee?)null);

            var service = BuildService(auth);

            await Assert.ThrowsAsync<ArgumentException>(() => service.ApplyLeaveAsync(ValidRequest(99)));
        }

        [Fact]
        public async Task ApplyLeaveAsync_ResolvesTargetEmployeesLinkedUserId_ForSelfOrPermissionCheck()
        {
            // The caller applies for employeeId 42, which is linked to User account 7 — LeaveService
            // must resolve that linkage and defer the self-vs-permission decision to
            // IAuthorizationService rather than trusting the raw employeeId from the request.
            var auth = new Mock<IAuthorizationService>();
            _employeeRepo.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(new Employee { Id = 42, UserId = 7 });
            _repo.Setup(r => r.GetLeaveTypeByIdAsync(2)).ReturnsAsync(
                new LeaveType { Id = 2, Code = LeaveTypeCodes.LeaveWithoutPay, Name = "LWP" });

            var service = BuildService(auth);
            await service.ApplyLeaveAsync(ValidRequest(42));

            auth.Verify(a => a.EnsureSelfOrPermission(7, PermissionModules.HrLeave), Times.Once);
        }

        [Fact]
        public async Task ApplyLeaveAsync_EmployeeWithNoLinkedUserAccount_FallsBackToPermissionCheckOnly()
        {
            // An employee with no login account (UserId null) can never be "self" for any caller,
            // so this must resolve to a sentinel that can't match a real CurrentUserId.
            var auth = new Mock<IAuthorizationService>();
            _employeeRepo.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(new Employee { Id = 42, UserId = null });
            _repo.Setup(r => r.GetLeaveTypeByIdAsync(2)).ReturnsAsync(
                new LeaveType { Id = 2, Code = LeaveTypeCodes.LeaveWithoutPay, Name = "LWP" });

            var service = BuildService(auth);
            await service.ApplyLeaveAsync(ValidRequest(42));

            auth.Verify(a => a.EnsureSelfOrPermission(-1, PermissionModules.HrLeave), Times.Once);
        }

        [Fact]
        public async Task ApplyLeaveAsync_WhenNeitherSelfNorPermitted_Throws()
        {
            var auth = new Mock<IAuthorizationService>();
            _employeeRepo.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(new Employee { Id = 42, UserId = 7 });
            auth.Setup(a => a.EnsureSelfOrPermission(7, PermissionModules.HrLeave))
                .Throws(new UnauthorizedAccessException("Access denied."));

            var service = BuildService(auth);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ApplyLeaveAsync(ValidRequest(42)));
        }
    }
}
