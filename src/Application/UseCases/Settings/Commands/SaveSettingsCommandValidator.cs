using FluentValidation;
using HotelPOS.Application.UseCases.Settings.Commands;

namespace HotelPOS.Application.UseCases.Settings.Commands
{
    public class SaveSettingsCommandValidator : AbstractValidator<SaveSettingsCommand>
    {
        public SaveSettingsCommandValidator()
        {
            RuleFor(x => x.Settings).NotNull().WithMessage("Settings object cannot be null.");

            RuleFor(x => x.Settings.HotelName)
                .NotEmpty().WithMessage("Hotel name cannot be empty.")
                .MaximumLength(200).WithMessage("Hotel name must not exceed 200 characters.")
                .When(x => x.Settings != null);

            RuleFor(x => x.Settings.HotelPhone)
                .MaximumLength(20).WithMessage("Hotel phone must not exceed 20 characters.")
                .When(x => x.Settings != null);

            RuleFor(x => x.Settings.HotelGst)
                .MaximumLength(15).WithMessage("GSTIN must not exceed 15 characters.")
                .When(x => x.Settings != null);
        }
    }
}
