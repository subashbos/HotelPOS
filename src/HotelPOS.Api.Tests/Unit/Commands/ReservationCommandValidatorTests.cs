using Xunit;
using FluentValidation.TestHelper;
using HotelPOS.Application.UseCases.Reservations.Commands;
using HotelPOS.Domain.Entities;
using System;

namespace HotelPOS.Tests.Unit.Commands
{
    public class ReservationCommandValidatorTests
    {
        // ---------- SaveReservationCommandValidator ----------
        [Fact]
        public void SaveReservationCommandValidator_NullReservation_HasError()
        {
            var validator = new SaveReservationCommandValidator();

            var result = validator.TestValidate(new SaveReservationCommand(null!));
            result.ShouldHaveValidationErrorFor(x => x.Reservation);
        }

        [Fact]
        public void SaveReservationCommandValidator_InvalidFields_HasErrors()
        {
            var validator = new SaveReservationCommandValidator();

            var badReservation = new Reservation
            {
                TableId = 0,
                PartySize = 0,
                ReservationDate = default,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(9, 0, 0) // before StartTime
            };
            var result = validator.TestValidate(new SaveReservationCommand(badReservation));

            result.ShouldHaveValidationErrorFor(x => x.Reservation.TableId);
            result.ShouldHaveValidationErrorFor(x => x.Reservation.PartySize);
            result.ShouldHaveValidationErrorFor(x => x.Reservation.ReservationDate);
            result.ShouldHaveValidationErrorFor(x => x.Reservation.EndTime);
        }

        [Fact]
        public void SaveReservationCommandValidator_EndTimeEqualsStartTime_HasError()
        {
            var validator = new SaveReservationCommandValidator();

            var reservation = new Reservation
            {
                TableId = 1,
                PartySize = 2,
                ReservationDate = DateTime.Today,
                StartTime = new TimeSpan(18, 0, 0),
                EndTime = new TimeSpan(18, 0, 0) // equal is not allowed (strictly greater required)
            };
            var result = validator.TestValidate(new SaveReservationCommand(reservation));
            result.ShouldHaveValidationErrorFor(x => x.Reservation.EndTime);
        }

        [Fact]
        public void SaveReservationCommandValidator_ValidReservation_HasNoErrors()
        {
            var validator = new SaveReservationCommandValidator();

            var goodReservation = new Reservation
            {
                TableId = 1,
                PartySize = 4,
                ReservationDate = DateTime.Today,
                StartTime = new TimeSpan(18, 0, 0),
                EndTime = new TimeSpan(20, 0, 0)
            };
            var result = validator.TestValidate(new SaveReservationCommand(goodReservation));
            result.ShouldNotHaveAnyValidationErrors();
        }

        // ---------- UpdateReservationCommandValidator ----------
        [Fact]
        public void UpdateReservationCommandValidator_NullReservation_HasError()
        {
            var validator = new UpdateReservationCommandValidator();

            var result = validator.TestValidate(new UpdateReservationCommand(null!));
            result.ShouldHaveValidationErrorFor(x => x.Reservation);
        }

        [Fact]
        public void UpdateReservationCommandValidator_InvalidFields_HasErrors()
        {
            var validator = new UpdateReservationCommandValidator();

            var badReservation = new Reservation
            {
                Id = 0,
                TableId = 0,
                PartySize = 0,
                ReservationDate = default,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(9, 0, 0) // before StartTime
            };
            var result = validator.TestValidate(new UpdateReservationCommand(badReservation));

            result.ShouldHaveValidationErrorFor(x => x.Reservation.Id);
            result.ShouldHaveValidationErrorFor(x => x.Reservation.TableId);
            result.ShouldHaveValidationErrorFor(x => x.Reservation.PartySize);
            result.ShouldHaveValidationErrorFor(x => x.Reservation.ReservationDate);
            result.ShouldHaveValidationErrorFor(x => x.Reservation.EndTime);
        }

        [Fact]
        public void UpdateReservationCommandValidator_ValidReservation_HasNoErrors()
        {
            var validator = new UpdateReservationCommandValidator();

            var goodReservation = new Reservation
            {
                Id = 1,
                TableId = 1,
                PartySize = 4,
                ReservationDate = DateTime.Today,
                StartTime = new TimeSpan(18, 0, 0),
                EndTime = new TimeSpan(20, 0, 0)
            };
            var result = validator.TestValidate(new UpdateReservationCommand(goodReservation));
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
