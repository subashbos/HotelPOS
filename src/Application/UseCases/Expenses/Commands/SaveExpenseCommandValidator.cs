using FluentValidation;

namespace HotelPOS.Application.UseCases.Expenses.Commands
{
    public class SaveExpenseCommandValidator : AbstractValidator<SaveExpenseCommand>
    {
        public SaveExpenseCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull().WithMessage("Expense data cannot be null.");

            RuleFor(x => x.Dto.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(200).WithMessage("Title must not exceed 200 characters.")
                .When(x => x.Dto != null);

            RuleFor(x => x.Dto.Category)
                .NotEmpty().WithMessage("Category is required.")
                .MaximumLength(50).WithMessage("Category must not exceed 50 characters.")
                .When(x => x.Dto != null);

            RuleFor(x => x.Dto.Amount)
                .GreaterThan(0).WithMessage("Amount must be greater than zero.")
                .When(x => x.Dto != null);

            RuleFor(x => x.Dto.Date)
                .NotEmpty().WithMessage("Date is required.")
                .When(x => x.Dto != null);

            RuleFor(x => x.Dto.PaymentMode)
                .MaximumLength(50).WithMessage("Payment mode must not exceed 50 characters.")
                .When(x => x.Dto != null && !string.IsNullOrWhiteSpace(x.Dto.PaymentMode));
        }
    }
}
