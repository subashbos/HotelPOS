using FluentValidation;

namespace HotelPOS.Application.UseCases.Suppliers.Commands
{
    public class DeleteSupplierCommandValidator : AbstractValidator<DeleteSupplierCommand>
    {
        public DeleteSupplierCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id must be greater than 0.");
        }
    }
}
