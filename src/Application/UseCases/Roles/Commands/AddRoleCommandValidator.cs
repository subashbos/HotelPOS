using FluentValidation;

namespace HotelPOS.Application.UseCases.Roles.Commands
{
    public class AddRoleCommandValidator : AbstractValidator<AddRoleCommand>
    {
        public AddRoleCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Role name cannot be empty.")
                .MaximumLength(50).WithMessage("Role name must not exceed 50 characters.");
        }
    }
}
