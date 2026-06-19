using FluentValidation;

namespace HotelPOS.Application.UseCases.Users.Commands
{
    public class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
    {
        public DeleteUserCommandValidator()
        {
            RuleFor(x => x.UserId).GreaterThan(0).WithMessage("Id must be greater than 0.");
        }
    }
}
