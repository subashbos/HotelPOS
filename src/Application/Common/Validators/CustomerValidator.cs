using FluentValidation;
using HotelPOS.Domain.Entities;
using System.Text.RegularExpressions;

namespace HotelPOS.Application.Common.Validators
{
    public class CustomerValidator : AbstractValidator<Customer>
    {
        public CustomerValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Customer Name is required.");

            RuleFor(x => x.Phone)
                .Must(phone =>
                {
                    if (string.IsNullOrWhiteSpace(phone)) return true;
                    var cleanPhone = Regex.Replace(phone, @"[^\d\+\-\(\)\s]", "", RegexOptions.None, TimeSpan.FromMilliseconds(250));
                    var digitCount = Regex.Replace(cleanPhone, @"[^\d]", "", RegexOptions.None, TimeSpan.FromMilliseconds(250)).Length;
                    return digitCount >= 10 && digitCount <= 15;
                })
                .WithMessage("Phone number must be a valid number between 10 and 15 digits.")
                .When(x => !string.IsNullOrWhiteSpace(x.Phone));

            RuleFor(x => x.Email)
                .Matches(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")
                .WithMessage("Email ID is invalid.")
                .When(x => !string.IsNullOrWhiteSpace(x.Email));

            RuleFor(x => x.Gstin)
                .Matches(@"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$")
                .WithMessage("GSTIN format is invalid.")
                .When(x => !string.IsNullOrWhiteSpace(x.Gstin));
        }
    }
}
