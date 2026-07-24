using FluentValidation;

namespace HotelPOS.Application.UseCases.UnitOfMeasurements.Commands
{
    public class UpdateUnitOfMeasurementCommandValidator : AbstractValidator<UpdateUnitOfMeasurementCommand>
    {
        public UpdateUnitOfMeasurementCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Invalid ID");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Unit name is required.");
        }
    }
}
