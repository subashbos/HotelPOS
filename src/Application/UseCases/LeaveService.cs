using FluentValidation;
using HotelPOS.Application.Common.Validators;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using HotelPOS.Domain.Events;
using MediatR;

namespace HotelPOS.Application.UseCases
{
    public class LeaveService : ILeaveService
    {
        private const string LeaveRequestEntityType = "LeaveRequest";

        private readonly ILeaveRepository _repository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IAuthorizationService _authorization;
        private readonly IValidator<LeaveRequest> _validator;
        private readonly IMediator? _mediator;

        public LeaveService(
            ILeaveRepository repository,
            IEmployeeRepository employeeRepository,
            IAuthorizationService authorization,
            IValidator<LeaveRequest>? validator = null,
            IMediator? mediator = null)
        {
            _repository = repository;
            _employeeRepository = employeeRepository;
            _authorization = authorization;
            _validator = validator ?? new LeaveRequestValidator();
            _mediator = mediator;
        }

        public async Task<List<LeaveType>> GetLeaveTypesAsync()
        {
            return await _repository.GetLeaveTypesAsync();
        }

        public async Task<List<LeaveBalance>> GetBalancesAsync(int employeeId, int year)
        {
            _authorization.EnsurePermission(PermissionModules.HrLeave);
            await EnsureBalancesInitializedAsync(employeeId, year);
            return await _repository.GetBalancesAsync(employeeId, year);
        }

        public async Task<List<LeaveRequest>> GetRequestsAsync(int? employeeId = null, string? status = null)
        {
            _authorization.EnsurePermission(PermissionModules.HrLeave);
            return await _repository.GetRequestsAsync(employeeId, status);
        }

        public async Task ApplyLeaveAsync(LeaveRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId)
                ?? throw new ArgumentException("The specified employee does not exist.");

            // A missing linked login account (UserId null) can never match a real CurrentUserId,
            // so this correctly falls through to the HrLeave permission check for such employees.
            _authorization.EnsureSelfOrPermission(employee.UserId ?? -1, PermissionModules.HrLeave);

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

                // Reserve the days immediately so a second, overlapping application can't also
                // pass this same check before either request is approved or rejected.
                balance.PendingDays += request.TotalDays;
                await _repository.UpdateBalanceAsync(balance);
            }

            await _repository.AddRequestAsync(request);

            if (_mediator != null)
            {
                await _mediator.Publish(new EntityActionEvent(
                    LeaveRequestEntityType, request.Id, "Create",
                    $"Employee: {request.EmployeeId}, Type: {leaveType.Name}, Days: {request.TotalDays}"));
            }
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
                // The days were already reserved in PendingDays when the request was applied for,
                // so approval just converts the hold into a committed usage — no fresh
                // insufficiency check is needed (or possible to race).
                var balance = await EnsureBalanceInitializedAsync(request.EmployeeId, leaveType, request.FromDate.Year);
                balance.PendingDays = Math.Max(0, balance.PendingDays - request.TotalDays);
                balance.UsedDays += request.TotalDays;
                await _repository.UpdateBalanceAsync(balance);
            }

            request.Status = LeaveRequestStatuses.Approved;
            request.ApprovedByEmployeeId = approverEmployeeId;
            request.ActionedOn = DateTime.UtcNow;
            await _repository.UpdateRequestAsync(request);

            if (_mediator != null)
            {
                await _mediator.Publish(new EntityActionEvent(
                    LeaveRequestEntityType, request.Id, "Update",
                    $"Status: {LeaveRequestStatuses.Approved}, Approver: {approverEmployeeId}"));
            }
        }

        public async Task RejectLeaveAsync(int requestId, int approverEmployeeId, string reason)
        {
            var request = await _repository.GetRequestByIdAsync(requestId)
                ?? throw new KeyNotFoundException($"Leave request #{requestId} not found.");

            if (request.Status != LeaveRequestStatuses.Pending)
                throw new InvalidOperationException("Only pending leave requests can be rejected.");

            var leaveType = request.LeaveType ?? await _repository.GetLeaveTypeByIdAsync(request.LeaveTypeId)
                ?? throw new ArgumentException("The selected leave type does not exist.");

            if (leaveType.Code != LeaveTypeCodes.LeaveWithoutPay)
            {
                // Release the hold placed on the balance when the request was applied for.
                var balance = await EnsureBalanceInitializedAsync(request.EmployeeId, leaveType, request.FromDate.Year);
                balance.PendingDays = Math.Max(0, balance.PendingDays - request.TotalDays);
                await _repository.UpdateBalanceAsync(balance);
            }

            request.Status = LeaveRequestStatuses.Rejected;
            request.ApprovedByEmployeeId = approverEmployeeId;
            request.ActionedOn = DateTime.UtcNow;
            request.RejectionReason = reason?.Trim();
            await _repository.UpdateRequestAsync(request);

            if (_mediator != null)
            {
                await _mediator.Publish(new EntityActionEvent(
                    LeaveRequestEntityType, request.Id, "Update",
                    $"Status: {LeaveRequestStatuses.Rejected}, Approver: {approverEmployeeId}"));
            }
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
