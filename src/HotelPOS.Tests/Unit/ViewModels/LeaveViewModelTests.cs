using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using HotelPOS.ViewModels;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.ViewModels
{
    public class LeaveViewModelTests
    {
        private readonly Mock<ILeaveService> _mockLeaveService = new();
        private readonly Mock<IEmployeeService> _mockEmployeeService = new();
        private readonly Mock<INotificationService> _mockNotif = new();
        private readonly LeaveViewModel _vm;

        public LeaveViewModelTests()
        {
            _mockLeaveService.Setup(s => s.GetBalancesAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<LeaveBalance>());
            _mockLeaveService.Setup(s => s.GetRequestsAsync(It.IsAny<int?>(), It.IsAny<string?>()))
                .ReturnsAsync(new List<LeaveRequest>());
            _vm = new LeaveViewModel(_mockLeaveService.Object, _mockEmployeeService.Object, _mockNotif.Object);
        }

        [Fact]
        public async Task InitializeAsync_LoadsEmployeesLeaveTypesAndRequests()
        {
            _mockEmployeeService.Setup(s => s.GetEmployeesAsync()).ReturnsAsync(new List<Employee>
            {
                new Employee { Id = 1, FirstName = "Alice" }
            });
            _mockLeaveService.Setup(s => s.GetLeaveTypesAsync()).ReturnsAsync(new List<LeaveType>
            {
                new LeaveType { Id = 1, Code = "CL", Name = "Casual Leave" }
            });
            _mockLeaveService.Setup(s => s.GetRequestsAsync(It.IsAny<int?>(), It.IsAny<string?>())).ReturnsAsync(new List<LeaveRequest>
            {
                new LeaveRequest { Id = 1, EmployeeId = 1 }
            });

            await _vm.InitializeAsync();

            Assert.Single(_vm.Employees);
            Assert.Equal("Alice", _vm.Employees[0].FirstName);
            Assert.Single(_vm.LeaveTypes);
            Assert.Equal("CL", _vm.LeaveTypes[0].Code);
            Assert.Single(_vm.Requests);
        }

        [Fact]
        public async Task InitializeAsync_WhenEmployeeServiceThrows_ShowsError()
        {
            _mockEmployeeService.Setup(s => s.GetEmployeesAsync()).ThrowsAsync(new InvalidOperationException("boom"));

            await _vm.InitializeAsync();

            _mockNotif.Verify(n => n.ShowError(It.Is<string>(s => s.Contains("Failed to load leave data") && s.Contains("boom"))), Times.Once);
        }

        [Fact]
        public async Task LoadRequestsAsync_PopulatesRequests()
        {
            _mockLeaveService.Setup(s => s.GetRequestsAsync(It.IsAny<int?>(), It.IsAny<string?>())).ReturnsAsync(new List<LeaveRequest>
            {
                new LeaveRequest { Id = 5 },
                new LeaveRequest { Id = 6 }
            });

            await _vm.LoadRequestsAsync();

            Assert.Equal(2, _vm.Requests.Count);
        }

        [Fact]
        public async Task LoadRequestsAsync_WhenThrows_ShowsError()
        {
            _mockLeaveService.Setup(s => s.GetRequestsAsync(It.IsAny<int?>(), It.IsAny<string?>()))
                .ThrowsAsync(new InvalidOperationException("db error"));

            await _vm.LoadRequestsAsync();

            _mockNotif.Verify(n => n.ShowError(It.Is<string>(s => s.Contains("Failed to load leave requests"))), Times.Once);
        }

        [Fact]
        public void SelectingEmployee_LoadsBalances()
        {
            _mockLeaveService.Setup(s => s.GetBalancesAsync(9, It.IsAny<int>())).ReturnsAsync(new List<LeaveBalance>
            {
                new LeaveBalance { Id = 1, EmployeeId = 9, EntitledDays = 12, UsedDays = 2 }
            });

            _vm.SelectedEmployee = new Employee { Id = 9, FirstName = "Bob" };

            Assert.Single(_vm.Balances);
            Assert.Equal(10, _vm.Balances[0].AvailableDays);
        }

        [Fact]
        public void SelectingNullEmployee_ClearsBalances()
        {
            _vm.SelectedEmployee = new Employee { Id = 1 };
            _vm.SelectedEmployee = null;

            Assert.Empty(_vm.Balances);
        }

        [Fact]
        public async Task ApplyAsync_NoEmployeeSelected_ShowsWarning()
        {
            _vm.SelectedEmployee = null;
            _vm.SelectedLeaveType = new LeaveType { Id = 1 };

            await _vm.ApplyCommand.ExecuteAsync(null);

            _mockNotif.Verify(n => n.ShowWarning(It.Is<string>(s => s.Contains("select an employee"))), Times.Once);
            _mockLeaveService.Verify(s => s.ApplyLeaveAsync(It.IsAny<LeaveRequest>()), Times.Never);
        }

        [Fact]
        public async Task ApplyAsync_NoLeaveTypeSelected_ShowsWarning()
        {
            _vm.SelectedEmployee = new Employee { Id = 1 };
            _vm.SelectedLeaveType = null;

            await _vm.ApplyCommand.ExecuteAsync(null);

            _mockNotif.Verify(n => n.ShowWarning(It.Is<string>(s => s.Contains("select a leave type"))), Times.Once);
            _mockLeaveService.Verify(s => s.ApplyLeaveAsync(It.IsAny<LeaveRequest>()), Times.Never);
        }

        [Fact]
        public async Task ApplyAsync_Success_AppliesLeaveClearsReasonAndReloads()
        {
            _vm.SelectedEmployee = new Employee { Id = 3 };
            _vm.SelectedLeaveType = new LeaveType { Id = 2 };
            _vm.Reason = "Family function";

            await _vm.ApplyCommand.ExecuteAsync(null);

            _mockLeaveService.Verify(s => s.ApplyLeaveAsync(It.Is<LeaveRequest>(r => r.EmployeeId == 3 && r.LeaveTypeId == 2)), Times.Once);
            _mockNotif.Verify(n => n.ShowSuccess(It.Is<string>(s => s.Contains("submitted"))), Times.Once);
            Assert.Null(_vm.Reason);
        }

        [Fact]
        public async Task ApplyAsync_ServiceThrows_ShowsErrorAndDoesNotReload()
        {
            _vm.SelectedEmployee = new Employee { Id = 3 };
            _vm.SelectedLeaveType = new LeaveType { Id = 2 };
            _mockLeaveService.Setup(s => s.ApplyLeaveAsync(It.IsAny<LeaveRequest>())).ThrowsAsync(new InvalidOperationException("apply failed"));

            await _vm.ApplyCommand.ExecuteAsync(null);

            _mockNotif.Verify(n => n.ShowError(It.Is<string>(s => s.Contains("apply failed"))), Times.Once);
        }

        [Fact]
        public async Task ApproveAsync_NullRequest_DoesNothing()
        {
            await _vm.ApproveCommand.ExecuteAsync(null);

            _mockLeaveService.Verify(s => s.ApproveLeaveAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task ApproveAsync_NoApprover_ShowsWarning()
        {
            _vm.ApproverEmployee = null;
            var request = new LeaveRequest { Id = 10 };

            await _vm.ApproveCommand.ExecuteAsync(request);

            _mockNotif.Verify(n => n.ShowWarning(It.Is<string>(s => s.Contains("approving"))), Times.Once);
            _mockLeaveService.Verify(s => s.ApproveLeaveAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task ApproveAsync_Success_ApprovesAndShowsSuccess()
        {
            _vm.ApproverEmployee = new Employee { Id = 99 };
            var request = new LeaveRequest { Id = 10 };

            await _vm.ApproveCommand.ExecuteAsync(request);

            _mockLeaveService.Verify(s => s.ApproveLeaveAsync(10, 99), Times.Once);
            _mockNotif.Verify(n => n.ShowSuccess(It.Is<string>(s => s.Contains("approved"))), Times.Once);
        }

        [Fact]
        public async Task ApproveAsync_ServiceThrows_ShowsError()
        {
            _vm.ApproverEmployee = new Employee { Id = 99 };
            var request = new LeaveRequest { Id = 10 };
            _mockLeaveService.Setup(s => s.ApproveLeaveAsync(10, 99)).ThrowsAsync(new InvalidOperationException("approve failed"));

            await _vm.ApproveCommand.ExecuteAsync(request);

            _mockNotif.Verify(n => n.ShowError(It.Is<string>(s => s.Contains("approve failed"))), Times.Once);
        }

        [Fact]
        public async Task RejectAsync_NullRequest_DoesNothing()
        {
            await _vm.RejectCommand.ExecuteAsync(null);

            _mockLeaveService.Verify(s => s.RejectLeaveAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RejectAsync_NoApprover_ShowsWarning()
        {
            _vm.ApproverEmployee = null;
            var request = new LeaveRequest { Id = 11 };

            await _vm.RejectCommand.ExecuteAsync(request);

            _mockNotif.Verify(n => n.ShowWarning(It.Is<string>(s => s.Contains("rejecting"))), Times.Once);
            _mockLeaveService.Verify(s => s.RejectLeaveAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RejectAsync_Success_RejectsClearsReasonAndShowsSuccess()
        {
            _vm.ApproverEmployee = new Employee { Id = 88 };
            _vm.RejectReasonText = "Not eligible";
            var request = new LeaveRequest { Id = 11 };

            await _vm.RejectCommand.ExecuteAsync(request);

            _mockLeaveService.Verify(s => s.RejectLeaveAsync(11, 88, "Not eligible"), Times.Once);
            _mockNotif.Verify(n => n.ShowSuccess(It.Is<string>(s => s.Contains("rejected"))), Times.Once);
            Assert.Null(_vm.RejectReasonText);
        }

        [Fact]
        public async Task RejectAsync_ServiceThrows_ShowsError()
        {
            _vm.ApproverEmployee = new Employee { Id = 88 };
            var request = new LeaveRequest { Id = 11 };
            _mockLeaveService.Setup(s => s.RejectLeaveAsync(11, 88, It.IsAny<string>())).ThrowsAsync(new InvalidOperationException("reject failed"));

            await _vm.RejectCommand.ExecuteAsync(request);

            _mockNotif.Verify(n => n.ShowError(It.Is<string>(s => s.Contains("reject failed"))), Times.Once);
        }
    }
}
