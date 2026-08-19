using FluentValidation;

namespace HotelPOS.Application.UseCases.UnitOfMeasurements.Commands
{
    public class CreateUnitOfMeasurementCommandValidator : AbstractValidator<CreateUnitOfMeasurementCommand>
    {
        public CreateUnitOfMeasurementCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Unit name is required.");
        }
    }
}
