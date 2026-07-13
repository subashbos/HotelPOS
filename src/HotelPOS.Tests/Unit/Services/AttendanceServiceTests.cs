using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.Services
{
    public class AttendanceServiceTests
    {
        private readonly Mock<IAttendanceRepository> _repoMock;
        private readonly AttendanceService _service;

        public AttendanceServiceTests()
        {
            _repoMock = new Mock<IAttendanceRepository>();
            _service = new AttendanceService(_repoMock.Object);
        }

        [Fact]
        public async Task MarkAttendanceAsync_WithCheckInAndOut_ComputesWorkedHours()
        {
            var attendance = new Attendance
            {
                EmployeeId = 1,
                Date = new DateTime(2026, 1, 10),
                Status = AttendanceStatuses.Present,
                CheckInTime = new TimeSpan(9, 0, 0),
                CheckOutTime = new TimeSpan(18, 30, 0)
            };

            _repoMock.Setup(r => r.GetByEmployeeAndDateAsync(1, attendance.Date)).ReturnsAsync((Attendance?)null);

            await _service.MarkAttendanceAsync(attendance);

            Assert.Equal(9.5m, attendance.WorkedHours);
            _repoMock.Verify(r => r.AddAsync(attendance), Times.Once);
        }

        [Fact]
        public async Task MarkAttendanceAsync_ExistingRecordForSameDay_UpdatesInsteadOfDuplicating()
        {
            var date = new DateTime(2026, 1, 10);
            var existing = new Attendance { Id = 7, EmployeeId = 1, Date = date, Status = AttendanceStatuses.Absent };
            var update = new Attendance { EmployeeId = 1, Date = date, Status = AttendanceStatuses.Present };

            _repoMock.Setup(r => r.GetByEmployeeAndDateAsync(1, date)).ReturnsAsync(existing);

            await _service.MarkAttendanceAsync(update);

            Assert.Equal(AttendanceStatuses.Present, existing.Status);
            _repoMock.Verify(r => r.UpdateAsync(existing), Times.Once);
            _repoMock.Verify(r => r.AddAsync(It.IsAny<Attendance>()), Times.Never);
        }

        [Fact]
        public async Task MarkAttendanceAsync_InvalidStatus_ThrowsArgumentException()
        {
            var attendance = new Attendance { EmployeeId = 1, Date = DateTime.Today, Status = "NotARealStatus" };

            await Assert.ThrowsAsync<ArgumentException>(() => _service.MarkAttendanceAsync(attendance));
        }

        [Fact]
        public async Task DeleteAttendanceAsync_NotFound_ThrowsKeyNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Attendance?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteAttendanceAsync(99));
        }
    }
}
