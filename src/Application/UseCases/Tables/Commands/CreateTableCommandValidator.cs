using FluentValidation;

namespace HotelPOS.Application.UseCases.Tables.Commands
{
    public class CreateTableCommandValidator : AbstractValidator<CreateTableCommand>
    {
        public CreateTableCommandValidator()
        {
            RuleFor(x => x.Dto.Number)
                .GreaterThan(0).WithMessage("Table number must be greater than zero.");
        }
    }
}
