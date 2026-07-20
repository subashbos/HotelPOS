using FluentValidation;

namespace HotelPOS.Application.UseCases.Expenses.Commands
{
    public class DeleteExpenseCommandValidator : AbstractValidator<DeleteExpenseCommand>
    {
        public DeleteExpenseCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id must be greater than 0.");
        }
    }
}
