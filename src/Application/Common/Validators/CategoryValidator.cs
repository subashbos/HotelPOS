using FluentValidation;
using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Common.Validators
{
    public class CategoryValidator : AbstractValidator<Category>
    {
        public CategoryValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Category name is required.");
        }
    }
}
