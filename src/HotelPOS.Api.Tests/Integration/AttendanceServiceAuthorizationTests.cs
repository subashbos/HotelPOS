using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class AttendanceServiceAuthorizationTests
    {
        private readonly Mock<IAttendanceRepository> _repo = new();

        private AttendanceService BuildService(Mock<IAuthorizationService> auth) =>
            new(_repo.Object, auth.Object);

        [Fact]
        public async Task GetAttendanceAsync_WhenUnauthorized_Throws()
        {
            var auth = TestAuthorization.DenyAll();
            var service = BuildService(auth);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => service.GetAttendanceAsync(1, DateTime.Today, DateTime.Today));
        }

        [Fact]
        public async Task GetAttendanceAsync_DoesNotAllowViewingAnotherEmployeesRecordWithoutPermission()
        {
            // Bare authentication with no HrAttendance grant must not be enough to view ANY
            // employee's attendance, including one's own — this module has no self-service path.
            var auth = new Mock<IAuthorizationService>();
            auth.Setup(a => a.EnsurePermission(PermissionModules.HrAttendance))
                .Throws(new UnauthorizedAccessException("Access denied."));

            var service = BuildService(auth);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => service.GetAttendanceAsync(42, DateTime.Today, DateTime.Today));
        }

        [Fact]
        public async Task GetAttendanceAsync_WhenGrantedHrAttendance_ReturnsAnyEmployeesRecords()
        {
            var auth = TestAuthorization.AllowAll();
            var from = new DateTime(2026, 1, 1);
            var to = new DateTime(2026, 1, 31);
            _repo.Setup(r => r.GetByEmployeeAsync(42, from, to)).ReturnsAsync(new List<Attendance>
            {
                new Attendance { Id = 1, EmployeeId = 42 }
            });

            var service = BuildService(auth);
            var result = await service.GetAttendanceAsync(42, from, to);

            Assert.Single(result);
            auth.Verify(a => a.EnsurePermission(PermissionModules.HrAttendance), Times.Once);
        }
    }
}
