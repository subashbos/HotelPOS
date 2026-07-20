using FluentValidation;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Common.Validators
{
    public class CashSessionValidator : AbstractValidator<CashSession>
    {
        public CashSessionValidator()
        {
            RuleFor(x => x.OpeningBalance)
                .GreaterThanOrEqualTo(0).WithMessage("Opening balance cannot be negative.");

            RuleFor(x => x.ActualCash)
                .GreaterThanOrEqualTo(0).WithMessage("Actual cash cannot be negative.")
                .When(x => x.Status == CashSessionStatuses.Closed);
        }
    }
}
