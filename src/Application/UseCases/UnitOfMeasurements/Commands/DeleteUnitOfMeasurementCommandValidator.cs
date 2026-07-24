using FluentValidation;

namespace HotelPOS.Application.UseCases.UnitOfMeasurements.Commands
{
    public class DeleteUnitOfMeasurementCommandValidator : AbstractValidator<DeleteUnitOfMeasurementCommand>
    {
        public DeleteUnitOfMeasurementCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id must be greater than 0.");
        }
    }
}
