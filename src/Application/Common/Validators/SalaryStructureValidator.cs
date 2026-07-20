using FluentValidation;
using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Common.Validators
{
    public class SalaryStructureValidator : AbstractValidator<SalaryStructure>
    {
        public SalaryStructureValidator()
        {
            RuleFor(x => x.EmployeeId)
                .GreaterThan(0).WithMessage("A valid employee is required.");

            RuleFor(x => x.Basic)
                .GreaterThan(0).WithMessage("Basic pay must be greater than zero.");

            RuleFor(x => x.Hra).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Da).GreaterThanOrEqualTo(0);
            RuleFor(x => x.ConveyanceAllowance).GreaterThanOrEqualTo(0);
            RuleFor(x => x.MedicalAllowance).GreaterThanOrEqualTo(0);
            RuleFor(x => x.SpecialAllowance).GreaterThanOrEqualTo(0);

            RuleFor(x => x.EffectiveTo)
                .GreaterThanOrEqualTo(x => x.EffectiveFrom)
                .WithMessage("Effective To cannot be before Effective From.")
                .When(x => x.EffectiveTo.HasValue);
        }
    }
}
