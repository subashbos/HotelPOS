using FluentValidation;
using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Common.Validators
{
    public class UnitOfMeasurementValidator : AbstractValidator<UnitOfMeasurement>
    {
        public UnitOfMeasurementValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Unit name is required.");
        }
    }
}
