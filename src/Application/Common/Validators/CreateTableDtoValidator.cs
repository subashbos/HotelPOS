using FluentValidation;
using HotelPOS.Application.DTOs.Table;

namespace HotelPOS.Application.Common.Validators
{
    public class CreateTableDtoValidator : AbstractValidator<CreateTableDto>
    {
        public CreateTableDtoValidator()
        {
            RuleFor(x => x.Number)
                .GreaterThan(0).WithMessage("Table number must be greater than zero.");
        }
    }
}
