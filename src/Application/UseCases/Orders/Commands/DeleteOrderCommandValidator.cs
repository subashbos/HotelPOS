using FluentValidation;

namespace HotelPOS.Application.UseCases.Orders.Commands
{
    public class DeleteOrderCommandValidator : AbstractValidator<DeleteOrderCommand>
    {
        public DeleteOrderCommandValidator()
        {
            RuleFor(x => x.OrderId).GreaterThan(0).WithMessage("Id must be greater than 0.");
        }
    }
}
