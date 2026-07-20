using FluentValidation;

namespace HotelPOS.Application.UseCases.Purchases.Commands
{
    public class SavePurchaseCommandValidator : AbstractValidator<SavePurchaseCommand>
    {
        public SavePurchaseCommandValidator()
        {
            RuleFor(x => x.Purchase).NotNull().WithMessage("Purchase data cannot be null.");

            RuleFor(x => x.Purchase.SupplierId)
                .GreaterThan(0).WithMessage("A valid supplier must be selected.")
                .When(x => x.Purchase != null);

            RuleFor(x => x.Purchase.PurchaseItems)
                .NotEmpty().WithMessage("Purchase must contain at least one item.")
                .When(x => x.Purchase != null);

            RuleFor(x => x.Purchase.GrandTotal)
                .GreaterThan(0).WithMessage("Purchase grand total must be greater than zero.")
                .When(x => x.Purchase != null);

            RuleFor(x => x.Purchase.PurchaseDate)
                .NotEmpty().WithMessage("Purchase date is required.")
                .When(x => x.Purchase != null);

            RuleForEach(x => x.Purchase.PurchaseItems)
                .ChildRules(item =>
                {
                    item.RuleFor(i => i.Quantity)
                        .GreaterThan(0).WithMessage("Each item quantity must be at least 1.");
                    item.RuleFor(i => i.UnitPrice)
                        .GreaterThan(0).WithMessage("Each item unit price must be greater than zero.");
                })
                .When(x => x.Purchase?.PurchaseItems != null);
        }
    }
}
