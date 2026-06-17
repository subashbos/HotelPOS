using FluentValidation;

namespace HotelPOS.Application.UseCases.Categories.Commands
{
    public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
    {
        public UpdateCategoryCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Invalid ID");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Category name is required.");
        }
    }
}
