using FluentValidation;
using HotelPOS.Domain.Common.Constants;
using System.Linq;

namespace HotelPOS.Application.UseCases.Orders.Commands
{
    public class UpdateOrderCommandValidator : AbstractValidator<UpdateOrderCommand>
    {
        public UpdateOrderCommandValidator()
        {
            RuleFor(x => x.Order)
                .NotNull().WithMessage("Order data cannot be null.");

            RuleFor(x => x.Order.Id)
                .GreaterThan(0).WithMessage("Invalid order ID.")
                .When(x => x.Order != null);

            RuleFor(x => x.Order.Items)
                .NotEmpty().WithMessage("Cannot save an empty order.")
                .When(x => x.Order != null);

            RuleForEach(x => x.Order.Items).ChildRules(item =>
            {
                item.RuleFor(x => x.Price)
                    .GreaterThanOrEqualTo(0).WithMessage("Item price cannot be negative.");

                item.RuleFor(x => x.Quantity)
                    .GreaterThan(0).WithMessage("Item quantity must be at least 1.");
            }).When(x => x.Order?.Items != null);

            RuleFor(x => x.Order.DiscountAmount)
                .GreaterThanOrEqualTo(0).WithMessage("Discount cannot be negative.")
                .When(x => x.Order != null);

            RuleFor(x => x.Order.PaymentMode)
                .Must(x => PaymentModes.All.Contains(x))
                .WithMessage("Invalid payment mode. Allowed: Cash, Card, UPI")
                .When(x => x.Order != null);

            RuleFor(x => x.Order.OrderType)
                .Must(x => OrderTypes.All.Contains(x))
                .WithMessage("Invalid order type. Allowed: DineIn, Takeaway, Online")
                .When(x => x.Order != null);

            RuleFor(x => x.Order)
                .Must(x => x.OrderType != OrderTypes.DineIn || x.TableNumber > 0)
                .WithMessage("Invalid table number.")
                .When(x => x.Order != null);
        }
    }
}
