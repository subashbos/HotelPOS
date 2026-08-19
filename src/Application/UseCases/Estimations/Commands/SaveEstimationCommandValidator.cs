using FluentValidation;
using HotelPOS.Domain.Common.Constants;

namespace HotelPOS.Application.UseCases.Estimations.Commands
{
    public class SaveEstimationCommandValidator : AbstractValidator<SaveEstimationCommand>
    {
        public SaveEstimationCommandValidator()
        {
            RuleFor(x => x.Estimation).NotNull().WithMessage("Estimation data cannot be null.");

            RuleFor(x => x.Estimation.EstimationNumber)
                .NotEmpty().WithMessage("Estimation number is required.")
                .When(x => x.Estimation != null);

            RuleFor(x => x.Estimation.Status)
                .Must(s => EstimationStatuses.All.Contains(s)).WithMessage("Invalid estimation status.")
                .When(x => x.Estimation != null);

            RuleFor(x => x.Estimation.EstimationItems)
                .NotEmpty().WithMessage("Estimation must contain at least one item.")
                .When(x => x.Estimation != null);

            RuleFor(x => x.Estimation.GrandTotal)
                .GreaterThan(0).WithMessage("Estimation grand total must be greater than zero.")
                .When(x => x.Estimation != null);

            RuleFor(x => x.Estimation.EstimationDate)
                .NotEmpty().WithMessage("Estimation date is required.")
                .When(x => x.Estimation != null);

            RuleForEach(x => x.Estimation.EstimationItems)
                .ChildRules(item =>
                {
                    item.RuleFor(i => i.Quantity)
                        .GreaterThan(0).WithMessage("Each item quantity must be at least 1.");
                    item.RuleFor(i => i.UnitPrice)
                        .GreaterThan(0).WithMessage("Each item unit price must be greater than zero.");
                })
                .When(x => x.Estimation?.EstimationItems != null);
        }
    }
}
