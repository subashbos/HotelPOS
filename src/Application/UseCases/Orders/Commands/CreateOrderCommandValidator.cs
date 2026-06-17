using FluentValidation;
using System.Linq;

namespace HotelPOS.Application.UseCases.Orders.Commands
{
    public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
    {
        public CreateOrderCommandValidator()
        {
            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("Cannot save an empty order.");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(x => x.Price)
                    .GreaterThanOrEqualTo(0).WithMessage("Item price cannot be negative.");
                
                item.RuleFor(x => x.Quantity)
                    .GreaterThan(0).WithMessage("Item quantity must be at least 1.");
            });

            RuleFor(x => x.Discount)
                .GreaterThanOrEqualTo(0).WithMessage("Discount cannot be negative.");

            RuleFor(x => x)
                .Must(x => x.Discount <= (x.Items ?? new()).Sum(i => i.Price * i.Quantity))
                .WithMessage("Discount cannot exceed order subtotal.");

            RuleFor(x => x.PaymentMode)
                .Must(x => new[] { "Cash", "Card", "UPI" }.Contains(x))
                .WithMessage("Invalid payment mode. Allowed: Cash, Card, UPI");

            RuleFor(x => x.OrderType)
                .Must(x => new[] { "DineIn", "Takeaway", "Online" }.Contains(x))
                .WithMessage("Invalid order type. Allowed: DineIn, Takeaway, Online");

            RuleFor(x => x)
                .Must(x => x.OrderType != "DineIn" || x.TableNumber > 0)
                .WithMessage("Invalid table number.");
        }
    }
}
