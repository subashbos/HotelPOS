using FluentValidation;

namespace HotelPOS.Application.UseCases.Reservations.Commands
{
    public class SaveReservationCommandValidator : AbstractValidator<SaveReservationCommand>
    {
        public SaveReservationCommandValidator()
        {
            RuleFor(x => x.Reservation).NotNull().WithMessage("Reservation data cannot be null.");

            RuleFor(x => x.Reservation.TableId)
                .GreaterThan(0).WithMessage("A valid table must be selected.")
                .When(x => x.Reservation != null);

            RuleFor(x => x.Reservation.PartySize)
                .GreaterThan(0).WithMessage("Party size must be at least 1.")
                .When(x => x.Reservation != null);

            RuleFor(x => x.Reservation.ReservationDate)
                .NotEmpty().WithMessage("Reservation date is required.")
                .When(x => x.Reservation != null);

            RuleFor(x => x.Reservation.EndTime)
                .GreaterThan(x => x.Reservation.StartTime).WithMessage("End time must be after start time.")
                .When(x => x.Reservation != null);
        }
    }
}
