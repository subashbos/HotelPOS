using FluentValidation;
using HotelPOS.Application.DTOs.CashSession;

namespace HotelPOS.Application.UseCases.CashSessions.Commands
{
    public class OpenSessionCommandValidator : AbstractValidator<OpenSessionCommand>
    {
        public OpenSessionCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull().WithMessage("Session data cannot be null.");

            RuleFor(x => x.Dto.OpeningBalance)
                .GreaterThanOrEqualTo(0).WithMessage("Opening balance cannot be negative.")
                .When(x => x.Dto != null);

            RuleFor(x => x.Dto.OpenedBy)
                .NotEmpty().WithMessage("Opened by username is required.")
                .When(x => x.Dto != null);
        }
    }

    public class CloseSessionCommandValidator : AbstractValidator<CloseSessionCommand>
    {
        public CloseSessionCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull().WithMessage("Session data cannot be null.");

            RuleFor(x => x.Dto.ActualCash)
                .GreaterThanOrEqualTo(0).WithMessage("Actual cash amount cannot be negative.")
                .When(x => x.Dto != null);

            RuleFor(x => x.Dto.ClosedBy)
                .NotEmpty().WithMessage("Closed by username is required.")
                .When(x => x.Dto != null);
        }
    }
}
