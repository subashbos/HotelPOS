using FluentValidation;

namespace HotelPOS.Application.UseCases.Tables.Commands
{
    public class UpdateTableCommandValidator : AbstractValidator<UpdateTableCommand>
    {
        public UpdateTableCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Invalid ID");

            RuleFor(x => x.Dto.Number)
                .GreaterThan(0).WithMessage("Table number must be greater than zero.");
        }
    }
}
