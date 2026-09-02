using Xunit;
using FluentValidation.TestHelper;
using HotelPOS.Application.UseCases.Estimations.Commands;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using System;
using System.Collections.Generic;

namespace HotelPOS.Tests.Unit.Commands
{
    public class EstimationCommandValidatorTests
    {
        // ---------- SaveEstimationCommandValidator ----------
        [Fact]
        public void SaveEstimationCommandValidator_NullEstimation_HasError()
        {
            var validator = new SaveEstimationCommandValidator();

            var result = validator.TestValidate(new SaveEstimationCommand(null!));
            result.ShouldHaveValidationErrorFor(x => x.Estimation);
        }

        [Fact]
        public void SaveEstimationCommandValidator_InvalidFields_HasErrors()
        {
            var validator = new SaveEstimationCommandValidator();

            var badEstimation = new Estimation
            {
                EstimationNumber = "",
                Status = "Bogus",
                EstimationItems = new List<EstimationItem>(),
                GrandTotal = 0,
                EstimationDate = default
            };
            var result = validator.TestValidate(new SaveEstimationCommand(badEstimation));

            result.ShouldHaveValidationErrorFor(x => x.Estimation.EstimationNumber);
            result.ShouldHaveValidationErrorFor(x => x.Estimation.Status);
            result.ShouldHaveValidationErrorFor(x => x.Estimation.EstimationItems);
            result.ShouldHaveValidationErrorFor(x => x.Estimation.GrandTotal);
            result.ShouldHaveValidationErrorFor(x => x.Estimation.EstimationDate);
        }

        [Fact]
        public void SaveEstimationCommandValidator_InvalidItemFields_HasErrors()
        {
            var validator = new SaveEstimationCommandValidator();

            var badEstimation = new Estimation
            {
                EstimationNumber = "EST-001",
                Status = EstimationStatuses.Draft,
                EstimationDate = DateTime.Today,
                GrandTotal = 100,
                EstimationItems = new List<EstimationItem>
                {
                    new EstimationItem { ItemId = 1, ItemName = "Pizza", Quantity = 0, UnitPrice = 0 }
                }
            };
            var result = validator.TestValidate(new SaveEstimationCommand(badEstimation));

            result.ShouldHaveValidationErrorFor("Estimation.EstimationItems[0].Quantity");
            result.ShouldHaveValidationErrorFor("Estimation.EstimationItems[0].UnitPrice");
        }

        [Fact]
        public void SaveEstimationCommandValidator_ValidEstimation_HasNoErrors()
        {
            var validator = new SaveEstimationCommandValidator();

            var goodEstimation = new Estimation
            {
                EstimationNumber = "EST-001",
                Status = EstimationStatuses.Draft,
                EstimationDate = DateTime.Today,
                GrandTotal = 500,
                EstimationItems = new List<EstimationItem>
                {
                    new EstimationItem { ItemId = 1, ItemName = "Pizza", Quantity = 2, UnitPrice = 250 }
                }
            };
            var result = validator.TestValidate(new SaveEstimationCommand(goodEstimation));
            result.ShouldNotHaveAnyValidationErrors();
        }

        // ---------- UpdateEstimationCommandValidator ----------
        [Fact]
        public void UpdateEstimationCommandValidator_NullEstimation_HasError()
        {
            var validator = new UpdateEstimationCommandValidator();

            var result = validator.TestValidate(new UpdateEstimationCommand(null!));
            result.ShouldHaveValidationErrorFor(x => x.Estimation);
        }

        [Fact]
        public void UpdateEstimationCommandValidator_InvalidFields_HasErrors()
        {
            var validator = new UpdateEstimationCommandValidator();

            var badEstimation = new Estimation
            {
                Id = 0,
                EstimationNumber = "",
                Status = "Bogus",
                EstimationItems = new List<EstimationItem>(),
                GrandTotal = 0,
                EstimationDate = default
            };
            var result = validator.TestValidate(new UpdateEstimationCommand(badEstimation));

            result.ShouldHaveValidationErrorFor(x => x.Estimation.Id);
            result.ShouldHaveValidationErrorFor(x => x.Estimation.EstimationNumber);
            result.ShouldHaveValidationErrorFor(x => x.Estimation.Status);
            result.ShouldHaveValidationErrorFor(x => x.Estimation.EstimationItems);
            result.ShouldHaveValidationErrorFor(x => x.Estimation.GrandTotal);
            result.ShouldHaveValidationErrorFor(x => x.Estimation.EstimationDate);
        }

        [Fact]
        public void UpdateEstimationCommandValidator_ValidEstimation_HasNoErrors()
        {
            var validator = new UpdateEstimationCommandValidator();

            var goodEstimation = new Estimation
            {
                Id = 1,
                EstimationNumber = "EST-001",
                Status = EstimationStatuses.Sent,
                EstimationDate = DateTime.Today,
                GrandTotal = 500,
                EstimationItems = new List<EstimationItem>
                {
                    new EstimationItem { ItemId = 1, ItemName = "Pizza", Quantity = 2, UnitPrice = 250 }
                }
            };
            var result = validator.TestValidate(new UpdateEstimationCommand(goodEstimation));
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
