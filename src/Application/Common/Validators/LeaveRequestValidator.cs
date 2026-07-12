using FluentValidation;
using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Common.Validators
{
    public class LeaveRequestValidator : AbstractValidator<LeaveRequest>
    {
        public LeaveRequestValidator()
        {
            RuleFor(x => x.EmployeeId)
                .GreaterThan(0).WithMessage("A valid employee is required.");

            RuleFor(x => x.LeaveTypeId)
                .GreaterThan(0).WithMessage("A leave type is required.");

            RuleFor(x => x.ToDate)
                .GreaterThanOrEqualTo(x => x.FromDate)
                .WithMessage("To Date cannot be before From Date.");

            RuleFor(x => x.TotalDays)
                .GreaterThan(0).WithMessage("Total leave days must be greater than zero.");
        }
    }
}
