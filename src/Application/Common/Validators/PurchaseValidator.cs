using FluentValidation;
using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Common.Validators
{
    public class PurchaseValidator : AbstractValidator<Purchase>
    {
        public PurchaseValidator()
        {
            RuleFor(x => x.SupplierId)
                .GreaterThan(0).WithMessage("Supplier is required.");

            RuleFor(x => x.InvoiceNumber)
                .NotEmpty().WithMessage("Invoice number is required.");

            RuleFor(x => x.PurchaseItems)
                .NotEmpty().WithMessage("Purchase must contain at least one item.");

            RuleForEach(x => x.PurchaseItems).SetValidator(new PurchaseItemValidator());
        }
    }

    public class PurchaseItemValidator : AbstractValidator<PurchaseItem>
    {
        public PurchaseItemValidator()
        {
            RuleFor(x => x.ItemId)
                .GreaterThan(0).WithMessage("Invalid item selected.");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage(x => $"Quantity for item '{x.ItemName}' must be greater than zero.");

            RuleFor(x => x.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage(x => $"Price for item '{x.ItemName}' cannot be negative.");
        }
    }
}
