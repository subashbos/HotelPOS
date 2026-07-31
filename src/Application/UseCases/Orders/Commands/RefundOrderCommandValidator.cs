using FluentValidation;

namespace HotelPOS.Application.UseCases.Orders.Commands
{
    public class RefundOrderCommandValidator : AbstractValidator<RefundOrderCommand>
    {
        public RefundOrderCommandValidator()
        {
            RuleFor(x => x.OrderId)
                .GreaterThan(0).WithMessage("Invalid order ID.");

            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Reason for the refund is required.");

            RuleFor(x => x.ItemsToRefund)
                .NotEmpty().WithMessage("At least one item is required to refund.");

            RuleForEach(x => x.ItemsToRefund).ChildRules(item =>
            {
                item.RuleFor(x => x.ItemId)
                    .GreaterThan(0).WithMessage("Invalid item ID.");

                item.RuleFor(x => x.QuantityToRefund)
                    .GreaterThan(0).WithMessage("Quantity to refund must be at least 1.");
            }).When(x => x.ItemsToRefund != null);
        }
    }
}
