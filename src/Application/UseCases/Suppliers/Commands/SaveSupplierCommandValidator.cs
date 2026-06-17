using FluentValidation;
using HotelPOS.Application.DTOs.Supplier;

namespace HotelPOS.Application.UseCases.Suppliers.Commands
{
    public class SaveSupplierCommandValidator : AbstractValidator<SaveSupplierCommand>
    {
        public SaveSupplierCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull().WithMessage("Supplier data cannot be null.");

            RuleFor(x => x.Dto.Name)
                .NotEmpty().WithMessage("Supplier name cannot be empty.")
                .MaximumLength(200).WithMessage("Name must not exceed 200 characters.")
                .When(x => x.Dto != null);

            RuleFor(x => x.Dto.Phone)
                .Must(p => string.IsNullOrWhiteSpace(p) || (p.Length >= 6 && p.Length <= 15))
                .WithMessage("Phone number must be between 6 and 15 characters.")
                .When(x => x.Dto != null);

            RuleFor(x => x.Dto.Email)
                .EmailAddress().WithMessage("Please enter a valid email address.")
                .When(x => x.Dto != null && !string.IsNullOrWhiteSpace(x.Dto.Email));

            RuleFor(x => x.Dto.Gstin)
                .MaximumLength(15).WithMessage("GSTIN must not exceed 15 characters.")
                .When(x => x.Dto != null && !string.IsNullOrWhiteSpace(x.Dto.Gstin));

            RuleFor(x => x.Dto.OpeningBalance)
                .GreaterThanOrEqualTo(0).WithMessage("Opening balance cannot be negative.")
                .When(x => x.Dto != null);

            RuleFor(x => x.Dto.CreditLimit)
                .GreaterThanOrEqualTo(0).WithMessage("Credit limit cannot be negative.")
                .When(x => x.Dto != null);
        }
    }
}
