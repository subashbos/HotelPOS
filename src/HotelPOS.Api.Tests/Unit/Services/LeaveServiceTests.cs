using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using HotelPOS.Domain.Events;
using MediatR;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.Services
{
    public class LeaveServiceTests
    {
        private readonly Mock<ILeaveRepository> _repoMock;
        private readonly Mock<IEmployeeRepository> _employeeRepoMock;
        private readonly Mock<IMediator> _mediatorMock;
        private readonly LeaveService _service;

        public LeaveServiceTests()
        {
            _repoMock = new Mock<ILeaveRepository>();
            _employeeRepoMock = new Mock<IEmployeeRepository>();
            _employeeRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new Employee { Id = id });
            _mediatorMock = new Mock<IMediator>();
            _service = new LeaveService(_repoMock.Object, _employeeRepoMock.Object, TestAuthorization.AllowAll().Object, mediator: _mediatorMock.Object);
        }

        private static LeaveType CasualLeaveType() => new()
        {
            Id = 1,
            Code = LeaveTypeCodes.CasualLeave,
            Name = "Casual Leave",
            AnnualQuota = 12,
            IsPaid = true
        };

        [Fact]
        public async Task ApplyLeaveAsync_SufficientBalance_CreatesRequest()
        {
            var leaveType = CasualLeaveType();
            var balance = new LeaveBalance { EmployeeId = 5, LeaveTypeId = 1, EntitledDays = 12, UsedDays = 0 };
            _repoMock.Setup(r => r.GetLeaveTypeByIdAsync(1)).ReturnsAsync(leaveType);
            _repoMock.Setup(r => r.GetBalanceAsync(5, 1, It.IsAny<int>())).ReturnsAsync(balance);

            var request = new LeaveRequest
            {
                EmployeeId = 5,
                LeaveTypeId = 1,
                FromDate = new DateTime(2026, 1, 5),
                ToDate = new DateTime(2026, 1, 6)
            };

            await _service.ApplyLeaveAsync(request);

            Assert.Equal(2, request.TotalDays);
            Assert.Equal(LeaveRequestStatuses.Pending, request.Status);
            _repoMock.Verify(r => r.AddRequestAsync(request), Times.Once);

            // The applied-for days are reserved on the balance immediately, so a second
            // overlapping application can't also pass the balance check before this one
            // is actioned.
            Assert.Equal(2, balance.PendingDays);
            _repoMock.Verify(r => r.UpdateBalanceAsync(balance), Times.Once);
        }

        [Fact]
        public async Task ApplyLeaveAsync_InsufficientBalance_ThrowsInvalidOperationException()
        {
            var leaveType = CasualLeaveType();
            _repoMock.Setup(r => r.GetLeaveTypeByIdAsync(1)).ReturnsAsync(leaveType);
            _repoMock.Setup(r => r.GetBalanceAsync(5, 1, It.IsAny<int>()))
                .ReturnsAsync(new LeaveBalance { EmployeeId = 5, LeaveTypeId = 1, EntitledDays = 12, UsedDays = 11 });

            var request = new LeaveRequest
            {
                EmployeeId = 5,
                LeaveTypeId = 1,
                FromDate = new DateTime(2026, 1, 5),
                ToDate = new DateTime(2026, 1, 8) // 4 days requested, only 1 available
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ApplyLeaveAsync(request));
            _repoMock.Verify(r => r.AddRequestAsync(It.IsAny<LeaveRequest>()), Times.Never);
        }

        [Fact]
        public async Task ApplyLeaveAsync_OverlappingPendingRequest_SecondApplicationFails()
        {
            // Simulates two applications for the same balance: the first reserves the days
            // (PendingDays), so the second — checked against the same balance instance,
            // as a fresh fetch from the repository would be after the first's UpdateBalanceAsync —
            // must see them as unavailable rather than both passing the check.
            var leaveType = CasualLeaveType();
            var balance = new LeaveBalance { EmployeeId = 5, LeaveTypeId = 1, EntitledDays = 12, UsedDays = 5 };
            _repoMock.Setup(r => r.GetLeaveTypeByIdAsync(1)).ReturnsAsync(leaveType);
            _repoMock.Setup(r => r.GetBalanceAsync(5, 1, It.IsAny<int>())).ReturnsAsync(balance);

            var firstRequest = new LeaveRequest
            {
                EmployeeId = 5,
                LeaveTypeId = 1,
                FromDate = new DateTime(2026, 1, 5),
                ToDate = new DateTime(2026, 1, 9) // 5 days — leaves exactly 2 available
            };
            await _service.ApplyLeaveAsync(firstRequest);
            Assert.Equal(5, balance.PendingDays);

            var secondRequest = new LeaveRequest
            {
                EmployeeId = 5,
                LeaveTypeId = 1,
                FromDate = new DateTime(2026, 2, 1),
                ToDate = new DateTime(2026, 2, 3) // 3 days — only 2 remain after the first hold
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ApplyLeaveAsync(secondRequest));
        }

        [Fact]
        public async Task ApplyLeaveAsync_LeaveWithoutPay_SkipsBalanceCheck()
        {
            var lwp = new LeaveType { Id = 2, Code = LeaveTypeCodes.LeaveWithoutPay, Name = "LWP", AnnualQuota = 0, IsPaid = false };
            _repoMock.Setup(r => r.GetLeaveTypeByIdAsync(2)).ReturnsAsync(lwp);

            var request = new LeaveRequest
            {
                EmployeeId = 5,
                LeaveTypeId = 2,
                FromDate = new DateTime(2026, 1, 5),
                ToDate = new DateTime(2026, 1, 10)
            };

            await _service.ApplyLeaveAsync(request);

            _repoMock.Verify(r => r.GetBalanceAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
            _repoMock.Verify(r => r.AddRequestAsync(request), Times.Once);
        }

        [Fact]
        public async Task ApproveLeaveAsync_Pending_DeductsBalanceAndApproves()
        {
            var leaveType = CasualLeaveType();
            var request = new LeaveRequest
            {
                Id = 10,
                EmployeeId = 5,
                LeaveTypeId = 1,
                FromDate = new DateTime(2026, 1, 5),
                ToDate = new DateTime(2026, 1, 6),
                TotalDays = 2,
                Status = LeaveRequestStatuses.Pending,
                LeaveType = leaveType
            };
            // PendingDays = 2 simulates the hold placed on the balance when this request was applied for.
            var balance = new LeaveBalance { EmployeeId = 5, LeaveTypeId = 1, EntitledDays = 12, UsedDays = 0, PendingDays = 2 };

            _repoMock.Setup(r => r.GetRequestByIdAsync(10)).ReturnsAsync(request);
            _repoMock.Setup(r => r.GetBalanceAsync(5, 1, It.IsAny<int>())).ReturnsAsync(balance);

            await _service.ApproveLeaveAsync(10, approverEmployeeId: 1);

            Assert.Equal(LeaveRequestStatuses.Approved, request.Status);
            Assert.Equal(2, balance.UsedDays);
            Assert.Equal(0, balance.PendingDays);
            _repoMock.Verify(r => r.UpdateBalanceAsync(balance), Times.Once);
            _repoMock.Verify(r => r.UpdateRequestAsync(request), Times.Once);
            _mediatorMock.Verify(
                m => m.Publish(It.Is<EntityActionEvent>(e => e.EntityName == "LeaveRequest" && e.EntityId == 10 && e.Action == "Approve"), default),
                Times.Once);
        }

        [Fact]
        public async Task ApproveLeaveAsync_AlreadyActioned_ThrowsInvalidOperationException()
        {
            var request = new LeaveRequest { Id = 10, Status = LeaveRequestStatuses.Approved };
            _repoMock.Setup(r => r.GetRequestByIdAsync(10)).ReturnsAsync(request);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ApproveLeaveAsync(10, 1));
        }

        [Fact]
        public async Task RejectLeaveAsync_Pending_SetsRejectedWithReason()
        {
            var request = new LeaveRequest
            {
                Id = 10,
                EmployeeId = 5,
                LeaveTypeId = 1,
                TotalDays = 2,
                Status = LeaveRequestStatuses.Pending,
                LeaveType = CasualLeaveType()
            };
            // PendingDays = 2 simulates the hold placed on the balance when this request was applied for.
            var balance = new LeaveBalance { EmployeeId = 5, LeaveTypeId = 1, EntitledDays = 12, UsedDays = 0, PendingDays = 2 };
            _repoMock.Setup(r => r.GetRequestByIdAsync(10)).ReturnsAsync(request);
            _repoMock.Setup(r => r.GetBalanceAsync(5, 1, It.IsAny<int>())).ReturnsAsync(balance);

            await _service.RejectLeaveAsync(10, approverEmployeeId: 1, reason: "Short-staffed that week");

            Assert.Equal(LeaveRequestStatuses.Rejected, request.Status);
            Assert.Equal("Short-staffed that week", request.RejectionReason);
            Assert.Equal(0, balance.PendingDays);
            _repoMock.Verify(r => r.UpdateBalanceAsync(balance), Times.Once);
            _repoMock.Verify(r => r.UpdateRequestAsync(request), Times.Once);
            _mediatorMock.Verify(
                m => m.Publish(It.Is<EntityActionEvent>(e => e.EntityName == "LeaveRequest" && e.EntityId == 10 && e.Action == "Reject"), default),
                Times.Once);
        }
    }
}
