using FluentValidation;

namespace HotelPOS.Application.UseCases.Items.Commands
{
    public class UpdateItemCommandValidator : AbstractValidator<UpdateItemCommand>
    {
        public UpdateItemCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Invalid item ID.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Item name cannot be empty or whitespace.")
                .MaximumLength(200).WithMessage("Item name must not exceed 200 characters.");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Item price must be greater than zero.");

            RuleFor(x => x.TaxPercentage)
                .GreaterThanOrEqualTo(0).WithMessage("Tax percentage cannot be negative.");

            RuleFor(x => x.UnitId)
                .GreaterThan(0).WithMessage("Unit is required.");
        }
    }
}
