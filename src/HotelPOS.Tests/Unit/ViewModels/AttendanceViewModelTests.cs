using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using HotelPOS.ViewModels;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.ViewModels
{
    public class AttendanceViewModelTests
    {
        private readonly Mock<IAttendanceService> _mockAttendanceService = new();
        private readonly Mock<IEmployeeService> _mockEmployeeService = new();
        private readonly Mock<INotificationService> _mockNotif = new();
        private readonly AttendanceViewModel _vm;

        public AttendanceViewModelTests()
        {
            _mockAttendanceService.Setup(s => s.GetAttendanceForDateAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Attendance>());
            _vm = new AttendanceViewModel(_mockAttendanceService.Object, _mockEmployeeService.Object, _mockNotif.Object);
        }

        [Fact]
        public async Task InitializeAsync_LoadsOnlyActiveEmployees()
        {
            _mockEmployeeService.Setup(s => s.GetEmployeesAsync()).ReturnsAsync(new List<Employee>
            {
                new Employee { Id = 1, FirstName = "Active1", Status = EmployeeStatuses.Active },
                new Employee { Id = 2, FirstName = "Resigned1", Status = EmployeeStatuses.Resigned },
                new Employee { Id = 3, FirstName = "Active2", Status = EmployeeStatuses.Active }
            });

            await _vm.InitializeAsync();

            Assert.Equal(2, _vm.Employees.Count);
            Assert.DoesNotContain(_vm.Employees, e => e.Status == EmployeeStatuses.Resigned);
        }

        [Fact]
        public async Task InitializeAsync_EmployeeServiceThrows_ShowsError()
        {
            _mockEmployeeService.Setup(s => s.GetEmployeesAsync()).ThrowsAsync(new InvalidOperationException("emp fail"));

            await _vm.InitializeAsync();

            _mockNotif.Verify(n => n.ShowError(It.Is<string>(s => s.Contains("Failed to load employees") && s.Contains("emp fail"))), Times.Once);
        }

        [Fact]
        public async Task LoadAttendanceForDateAsync_PopulatesRecords()
        {
            _mockAttendanceService.Setup(s => s.GetAttendanceForDateAsync(It.IsAny<DateTime>())).ReturnsAsync(new List<Attendance>
            {
                new Attendance { Id = 1 },
                new Attendance { Id = 2 }
            });

            await _vm.LoadAttendanceForDateAsync();

            Assert.Equal(2, _vm.AttendanceRecords.Count);
        }

        [Fact]
        public async Task LoadAttendanceForDateAsync_Throws_ShowsError()
        {
            _mockAttendanceService.Setup(s => s.GetAttendanceForDateAsync(It.IsAny<DateTime>()))
                .ThrowsAsync(new InvalidOperationException("attendance fail"));

            await _vm.LoadAttendanceForDateAsync();

            _mockNotif.Verify(n => n.ShowError(It.Is<string>(s => s.Contains("Failed to load attendance"))), Times.Once);
        }

        [Fact]
        public void SelectingDate_TriggersReloadForNewDate()
        {
            var newDate = DateTime.Today.AddDays(-1);
            _mockAttendanceService.Setup(s => s.GetAttendanceForDateAsync(newDate)).ReturnsAsync(new List<Attendance>
            {
                new Attendance { Id = 42, Date = newDate }
            });

            _vm.SelectedDate = newDate;

            Assert.Single(_vm.AttendanceRecords);
            Assert.Equal(42, _vm.AttendanceRecords[0].Id);
        }

        [Fact]
        public async Task MarkAsync_NoEmployeeSelected_ShowsWarning()
        {
            _vm.SelectedEmployee = null;

            await _vm.MarkCommand.ExecuteAsync(null);

            _mockNotif.Verify(n => n.ShowWarning(It.Is<string>(s => s.Contains("select an employee"))), Times.Once);
            _mockAttendanceService.Verify(s => s.MarkAttendanceAsync(It.IsAny<Attendance>()), Times.Never);
        }

        [Fact]
        public async Task MarkAsync_Success_ParsesTimesClearsFieldsAndReloads()
        {
            _vm.SelectedEmployee = new Employee { Id = 5, FirstName = "Dave" };
            _vm.Status = AttendanceStatuses.Present;
            _vm.CheckInText = "09:00";
            _vm.CheckOutText = "18:30";
            _vm.Remarks = "On time";

            Attendance? captured = null;
            _mockAttendanceService.Setup(s => s.MarkAttendanceAsync(It.IsAny<Attendance>()))
                .Callback<Attendance>(a => captured = a)
                .ReturnsAsync((Attendance a) => a);

            await _vm.MarkCommand.ExecuteAsync(null);

            Assert.NotNull(captured);
            Assert.Equal(5, captured!.EmployeeId);
            Assert.Equal(new TimeSpan(9, 0, 0), captured.CheckInTime);
            Assert.Equal(new TimeSpan(18, 30, 0), captured.CheckOutTime);
            Assert.Equal("On time", captured.Remarks);

            _mockNotif.Verify(n => n.ShowSuccess(It.Is<string>(s => s.Contains("Dave"))), Times.Once);
            Assert.Equal(string.Empty, _vm.CheckInText);
            Assert.Equal(string.Empty, _vm.CheckOutText);
            Assert.Null(_vm.Remarks);
        }

        [Fact]
        public async Task MarkAsync_InvalidTimeText_ResultsInNullTimes()
        {
            _vm.SelectedEmployee = new Employee { Id = 6, FirstName = "Eve" };
            _vm.CheckInText = "not-a-time";
            _vm.CheckOutText = "";

            Attendance? captured = null;
            _mockAttendanceService.Setup(s => s.MarkAttendanceAsync(It.IsAny<Attendance>()))
                .Callback<Attendance>(a => captured = a)
                .ReturnsAsync((Attendance a) => a);

            await _vm.MarkCommand.ExecuteAsync(null);

            Assert.NotNull(captured);
            Assert.Null(captured!.CheckInTime);
            Assert.Null(captured.CheckOutTime);
        }

        [Fact]
        public async Task MarkAsync_ServiceThrows_ShowsError()
        {
            _vm.SelectedEmployee = new Employee { Id = 7, FirstName = "Frank" };
            _mockAttendanceService.Setup(s => s.MarkAttendanceAsync(It.IsAny<Attendance>()))
                .ThrowsAsync(new InvalidOperationException("mark fail"));

            await _vm.MarkCommand.ExecuteAsync(null);

            _mockNotif.Verify(n => n.ShowError(It.Is<string>(s => s.Contains("mark fail"))), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_NullAttendance_DoesNothing()
        {
            await _vm.DeleteCommand.ExecuteAsync(null);

            _mockAttendanceService.Verify(s => s.DeleteAttendanceAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_Success_DeletesAndReloads()
        {
            var attendance = new Attendance { Id = 15 };
            _mockAttendanceService.Setup(s => s.GetAttendanceForDateAsync(It.IsAny<DateTime>())).ReturnsAsync(new List<Attendance>());

            await _vm.DeleteCommand.ExecuteAsync(attendance);

            _mockAttendanceService.Verify(s => s.DeleteAttendanceAsync(15), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ServiceThrows_ShowsError()
        {
            var attendance = new Attendance { Id = 16 };
            _mockAttendanceService.Setup(s => s.DeleteAttendanceAsync(16)).ThrowsAsync(new InvalidOperationException("delete fail"));

            await _vm.DeleteCommand.ExecuteAsync(attendance);

            _mockNotif.Verify(n => n.ShowError(It.Is<string>(s => s.Contains("delete fail"))), Times.Once);
        }
    }
}
