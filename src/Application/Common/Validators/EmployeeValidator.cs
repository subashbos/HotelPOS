using FluentValidation;
using HotelPOS.Domain.Entities;
using System.Text.RegularExpressions;

namespace HotelPOS.Application.Common.Validators
{
    public class EmployeeValidator : AbstractValidator<Employee>
    {
        public EmployeeValidator()
        {
            RuleFor(x => x.EmployeeCode)
                .NotEmpty().WithMessage("Employee Code is required.");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First Name is required.");

            RuleFor(x => x.DateOfJoining)
                .NotEqual(default(DateTime)).WithMessage("Date of Joining is required.");

            RuleFor(x => x.DateOfExit)
                .GreaterThanOrEqualTo(x => x.DateOfJoining)
                .WithMessage("Date of Exit cannot be before Date of Joining.")
                .When(x => x.DateOfExit.HasValue);

            RuleFor(x => x.Phone)
                .Must(phone =>
                {
                    if (string.IsNullOrWhiteSpace(phone)) return true;
                    var digitCount = Regex.Replace(phone, @"[^\d]", "", RegexOptions.None, TimeSpan.FromMilliseconds(250)).Length;
                    return digitCount >= 10 && digitCount <= 15;
                })
                .WithMessage("Phone number must be a valid number between 10 and 15 digits.")
                .When(x => !string.IsNullOrWhiteSpace(x.Phone));

            RuleFor(x => x.Email)
                .Matches(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")
                .WithMessage("Email ID is invalid.")
                .When(x => !string.IsNullOrWhiteSpace(x.Email));

            RuleFor(x => x.Pan)
                .Matches(@"^[A-Z]{5}[0-9]{4}[A-Z]{1}$")
                .WithMessage("PAN format is invalid (expected AAAAA9999A).")
                .When(x => !string.IsNullOrWhiteSpace(x.Pan));

            RuleFor(x => x.Aadhaar)
                .Matches(@"^\d{12}$")
                .WithMessage("Aadhaar must be a 12-digit number.")
                .When(x => !string.IsNullOrWhiteSpace(x.Aadhaar));

            RuleFor(x => x.BankIfsc)
                .Matches(@"^[A-Z]{4}0[A-Z0-9]{6}$")
                .WithMessage("IFSC format is invalid (expected AAAA0999999).")
                .When(x => !string.IsNullOrWhiteSpace(x.BankIfsc));
        }
    }
}
