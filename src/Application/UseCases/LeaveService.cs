using FluentValidation;
using HotelPOS.Application.Common.Validators;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.UseCases
{
    public class LeaveService : ILeaveService
    {
        private readonly ILeaveRepository _repository;
        private readonly IValidator<LeaveRequest> _validator;

        public LeaveService(ILeaveRepository repository, IValidator<LeaveRequest>? validator = null)
        {
            _repository = repository;
            _validator = validator ?? new LeaveRequestValidator();
        }

        public async Task<List<LeaveType>> GetLeaveTypesAsync()
        {
            return await _repository.GetLeaveTypesAsync();
        }

        public async Task<List<LeaveBalance>> GetBalancesAsync(int employeeId, int year)
        {
            await EnsureBalancesInitializedAsync(employeeId, year);
            return await _repository.GetBalancesAsync(employeeId, year);
        }

        public async Task<List<LeaveRequest>> GetRequestsAsync(int? employeeId = null, string? status = null)
        {
            return await _repository.GetRequestsAsync(employeeId, status);
        }

        public async Task ApplyLeaveAsync(LeaveRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            request.FromDate = request.FromDate.Date;
            request.ToDate = request.ToDate.Date;
            if (request.TotalDays <= 0)
                request.TotalDays = (decimal)(request.ToDate - request.FromDate).Days + 1;
            request.Reason = request.Reason?.Trim();
            request.Status = LeaveRequestStatuses.Pending;
            request.AppliedOn = DateTime.UtcNow;

            var result = _validator.Validate(request);
            if (!result.IsValid)
                throw new ArgumentException(result.Errors[0].ErrorMessage);

            var leaveType = await _repository.GetLeaveTypeByIdAsync(request.LeaveTypeId)
                ?? throw new ArgumentException("The selected leave type does not exist.");

            if (leaveType.Code != LeaveTypeCodes.LeaveWithoutPay)
            {
                var balance = await EnsureBalanceInitializedAsync(request.EmployeeId, leaveType, request.FromDate.Year);
                if (balance.AvailableDays < request.TotalDays)
                    throw new InvalidOperationException(
                        $"Insufficient {leaveType.Name} balance: {balance.AvailableDays} day(s) available, {request.TotalDays} requested.");
            }

            await _repository.AddRequestAsync(request);
        }

        public async Task ApproveLeaveAsync(int requestId, int approverEmployeeId)
        {
            var request = await _repository.GetRequestByIdAsync(requestId)
                ?? throw new KeyNotFoundException($"Leave request #{requestId} not found.");

            if (request.Status != LeaveRequestStatuses.Pending)
                throw new InvalidOperationException("Only pending leave requests can be approved.");

            var leaveType = request.LeaveType ?? await _repository.GetLeaveTypeByIdAsync(request.LeaveTypeId)
                ?? throw new ArgumentException("The selected leave type does not exist.");

            if (leaveType.Code != LeaveTypeCodes.LeaveWithoutPay)
            {
                var balance = await EnsureBalanceInitializedAsync(request.EmployeeId, leaveType, request.FromDate.Year);
                if (balance.AvailableDays < request.TotalDays)
                    throw new InvalidOperationException(
                        $"Insufficient {leaveType.Name} balance: {balance.AvailableDays} day(s) available, {request.TotalDays} requested.");

                balance.UsedDays += request.TotalDays;
                await _repository.UpdateBalanceAsync(balance);
            }

            request.Status = LeaveRequestStatuses.Approved;
            request.ApprovedByEmployeeId = approverEmployeeId;
            request.ActionedOn = DateTime.UtcNow;
            await _repository.UpdateRequestAsync(request);
        }

        public async Task RejectLeaveAsync(int requestId, int approverEmployeeId, string reason)
        {
            var request = await _repository.GetRequestByIdAsync(requestId)
                ?? throw new KeyNotFoundException($"Leave request #{requestId} not found.");

            if (request.Status != LeaveRequestStatuses.Pending)
                throw new InvalidOperationException("Only pending leave requests can be rejected.");

            request.Status = LeaveRequestStatuses.Rejected;
            request.ApprovedByEmployeeId = approverEmployeeId;
            request.ActionedOn = DateTime.UtcNow;
            request.RejectionReason = reason?.Trim();
            await _repository.UpdateRequestAsync(request);
        }

        private async Task EnsureBalancesInitializedAsync(int employeeId, int year)
        {
            var leaveTypes = await _repository.GetLeaveTypesAsync();
            foreach (var leaveType in leaveTypes)
            {
                await EnsureBalanceInitializedAsync(employeeId, leaveType, year);
            }
        }

        private async Task<LeaveBalance> EnsureBalanceInitializedAsync(int employeeId, LeaveType leaveType, int year)
        {
            var balance = await _repository.GetBalanceAsync(employeeId, leaveType.Id, year);
            if (balance != null) return balance;

            balance = new LeaveBalance
            {
                EmployeeId = employeeId,
                LeaveTypeId = leaveType.Id,
                Year = year,
                EntitledDays = leaveType.AnnualQuota,
                UsedDays = 0
            };
            await _repository.AddBalanceAsync(balance);
            balance.LeaveType = leaveType;
            return balance;
        }
    }
}
