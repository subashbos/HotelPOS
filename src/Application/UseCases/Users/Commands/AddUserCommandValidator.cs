using FluentValidation;

namespace HotelPOS.Application.UseCases.Users.Commands
{
    public class AddUserCommandValidator : AbstractValidator<AddUserCommand>
    {
        private const int MinimumPasswordLength = 10;

        public AddUserCommandValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username cannot be empty.")
                .MaximumLength(50).WithMessage("Username must not exceed 50 characters.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password cannot be empty.")
                .MinimumLength(MinimumPasswordLength)
                .WithMessage($"Password must be at least {MinimumPasswordLength} characters.");

            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("Role cannot be empty.");
        }
    }
}
