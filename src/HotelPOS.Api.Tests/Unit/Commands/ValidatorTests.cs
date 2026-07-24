using Xunit;
using FluentValidation.TestHelper;
using HotelPOS.Application.UseCases.Suppliers.Commands;
using HotelPOS.Application.UseCases.Expenses.Commands;
using HotelPOS.Application.UseCases.Purchases.Commands;
using HotelPOS.Application.UseCases.Users.Commands;
using HotelPOS.Application.UseCases.Settings.Commands;
using HotelPOS.Application.UseCases.Items.Commands;
using HotelPOS.Application.Common.Validators;
using HotelPOS.Application.DTOs.Supplier;
using HotelPOS.Application.DTOs.Expense;
using HotelPOS.Domain.Entities;
using System;
using System.Collections.Generic;

namespace HotelPOS.Tests.Unit.Commands
{
    public class ValidatorTests
    {
        // ---------- SaveSupplierCommandValidator ----------
        [Fact]
        public void SaveSupplierCommandValidator_Validates_Correctly()
        {
            var validator = new SaveSupplierCommandValidator();

            // Null Dto
            var cmdNull = new SaveSupplierCommand(null!);
            var resNull = validator.TestValidate(cmdNull);
            resNull.ShouldHaveValidationErrorFor(x => x.Dto);

            // Empty fields / Invalid fields
            var badDto = new SaveSupplierDto
            {
                Name = "",
                Phone = "123", // too short
                Email = "not-an-email",
                OpeningBalance = -10,
                CreditLimit = -100
            };
            var resBad = validator.TestValidate(new SaveSupplierCommand(badDto));
            resBad.ShouldHaveValidationErrorFor(x => x.Dto.Name);
            resBad.ShouldHaveValidationErrorFor(x => x.Dto.Phone);
            resBad.ShouldHaveValidationErrorFor(x => x.Dto.Email);
            resBad.ShouldHaveValidationErrorFor(x => x.Dto.OpeningBalance);
            resBad.ShouldHaveValidationErrorFor(x => x.Dto.CreditLimit);

            // Valid fields
            var goodDto = new SaveSupplierDto
            {
                Name = "Fresh Vendor",
                Phone = "9876543210",
                Email = "vendor@test.com",
                OpeningBalance = 0,
                CreditLimit = 10000
            };
            var resGood = validator.TestValidate(new SaveSupplierCommand(goodDto));
            resGood.ShouldNotHaveAnyValidationErrors();
        }

        // ---------- SaveExpenseCommandValidator ----------
        [Fact]
        public void SaveExpenseCommandValidator_Validates_Correctly()
        {
            var validator = new SaveExpenseCommandValidator();

            // Null Dto
            var resNull = validator.TestValidate(new SaveExpenseCommand(null!));
            resNull.ShouldHaveValidationErrorFor(x => x.Dto);

            // Empty / Invalid
            var badDto = new SaveExpenseDto
            {
                Title = "",
                Category = "",
                Amount = -5,
                PaymentMode = new string('a', 51)
            };
            var resBad = validator.TestValidate(new SaveExpenseCommand(badDto));
            resBad.ShouldHaveValidationErrorFor(x => x.Dto.Title);
            resBad.ShouldHaveValidationErrorFor(x => x.Dto.Category);
            resBad.ShouldHaveValidationErrorFor(x => x.Dto.Amount);
            resBad.ShouldHaveValidationErrorFor(x => x.Dto.PaymentMode);

            // Valid
            var goodDto = new SaveExpenseDto
            {
                Title = "Vegies",
                Category = "Kitchen",
                Amount = 1500,
                Date = DateTime.Today,
                PaymentMode = "Cash"
            };
            var resGood = validator.TestValidate(new SaveExpenseCommand(goodDto));
            resGood.ShouldNotHaveAnyValidationErrors();
        }

        // ---------- SupplierValidator ----------
        [Fact]
        public void SupplierValidator_Validates_Correctly()
        {
            var validator = new SupplierValidator();

            // Invalid fields
            var badSupplier = new Supplier
            {
                Name = "",
                Phone = "123",
                Email = "plainaddress",
                Gstin = "invalid_gst"
            };
            var resBad = validator.TestValidate(badSupplier);
            resBad.ShouldHaveValidationErrorFor(x => x.Name);
            resBad.ShouldHaveValidationErrorFor(x => x.Phone);
            resBad.ShouldHaveValidationErrorFor(x => x.Email);
            resBad.ShouldHaveValidationErrorFor(x => x.Gstin);

            // Valid fields
            var goodSupplier = new Supplier
            {
                Name = "Metro",
                Phone = "9876543210",
                Email = "metro@wholesale.com",
                Gstin = "27AABCU9603R1ZX" // Valid Indian GSTIN format
            };
            var resGood = validator.TestValidate(goodSupplier);
            resGood.ShouldNotHaveAnyValidationErrors();
        }

        // ---------- SavePurchaseCommandValidator ----------
        [Fact]
        public void SavePurchaseCommandValidator_Validates_Correctly()
        {
            var validator = new SavePurchaseCommandValidator();

            // Null Purchase
            var resNull = validator.TestValidate(new SavePurchaseCommand(null!));
            resNull.ShouldHaveValidationErrorFor(x => x.Purchase);

            // Invalid
            var badPurchase = new Purchase
            {
                SupplierId = 0,
                GrandTotal = -10,
                PurchaseItems = new List<PurchaseItem>
                {
                    new PurchaseItem { Quantity = 0, UnitPrice = -5 }
                }
            };
            var resBad = validator.TestValidate(new SavePurchaseCommand(badPurchase));
            resBad.ShouldHaveValidationErrorFor(x => x.Purchase.SupplierId);
            resBad.ShouldHaveValidationErrorFor(x => x.Purchase.GrandTotal);
            resBad.ShouldHaveValidationErrorFor("Purchase.PurchaseItems[0].Quantity");
            resBad.ShouldHaveValidationErrorFor("Purchase.PurchaseItems[0].UnitPrice");

            // Valid
            var goodPurchase = new Purchase
            {
                SupplierId = 5,
                GrandTotal = 1500,
                PurchaseDate = DateTime.Today,
                PurchaseItems = new List<PurchaseItem>
                {
                    new PurchaseItem { ItemId = 1, Quantity = 10, UnitPrice = 150 }
                }
            };
            var resGood = validator.TestValidate(new SavePurchaseCommand(goodPurchase));
            resGood.ShouldNotHaveAnyValidationErrors();
        }

        // ---------- AddUserCommandValidator ----------
        [Fact]
        public void AddUserCommandValidator_Validates_Correctly()
        {
            var validator = new AddUserCommandValidator();

            // Invalid fields
            var badCmd = new AddUserCommand("", "", "", 0);
            var resBad = validator.TestValidate(badCmd);
            resBad.ShouldHaveValidationErrorFor(x => x.Username);
            resBad.ShouldHaveValidationErrorFor(x => x.Password);
            resBad.ShouldHaveValidationErrorFor(x => x.Role);

            // Invalid password complexity (e.g. no special char or digit or uppercase)
            var simplePasswordCmd = new AddUserCommand("admin", "plainpassword", "Admin", 1);
            var resSimple = validator.TestValidate(simplePasswordCmd);
            resSimple.ShouldHaveValidationErrorFor(x => x.Password);

            // Valid fields
            var goodCmd = new AddUserCommand("admin", "Admin123!@#", "Admin", 1);
            var resGood = validator.TestValidate(goodCmd);
            resGood.ShouldNotHaveAnyValidationErrors();
        }

        // ---------- SaveSettingsCommandValidator ----------
        [Fact]
        public void SaveSettingsCommandValidator_Validates_Correctly()
        {
            var validator = new SaveSettingsCommandValidator();

            // Null settings
            var resNull = validator.TestValidate(new SaveSettingsCommand(null!));
            resNull.ShouldHaveValidationErrorFor(x => x.Settings);

            // Invalid fields
            var badSettings = new SystemSetting
            {
                HotelName = "",
                HotelPhone = "123456789012345678901", // 21 chars
                HotelGst = "1234567890123456" // 16 chars
            };
            var resBad = validator.TestValidate(new SaveSettingsCommand(badSettings));
            resBad.ShouldHaveValidationErrorFor(x => x.Settings.HotelName);
            resBad.ShouldHaveValidationErrorFor(x => x.Settings.HotelPhone);
            resBad.ShouldHaveValidationErrorFor(x => x.Settings.HotelGst);

            // Valid
            var goodSettings = new SystemSetting
            {
                HotelName = "Grand Plaza",
                HotelPhone = "1234567890",
                HotelGst = "GSTIN123"
            };
            var resGood = validator.TestValidate(new SaveSettingsCommand(goodSettings));
            resGood.ShouldNotHaveAnyValidationErrors();
        }

        // ---------- UpdateItemCommandValidator ----------
        [Fact]
        public void UpdateItemCommandValidator_Validates_Correctly()
        {
            var validator = new UpdateItemCommandValidator();

            // Invalid fields
            var badCmd = new UpdateItemCommand(0, "", 0, -5, null, null, null, 0, false, 0);
            var resBad = validator.TestValidate(badCmd);
            resBad.ShouldHaveValidationErrorFor(x => x.Id);
            resBad.ShouldHaveValidationErrorFor(x => x.Name);
            resBad.ShouldHaveValidationErrorFor(x => x.Price);
            resBad.ShouldHaveValidationErrorFor(x => x.TaxPercentage);

            // Valid
            var goodCmd = new UpdateItemCommand(1, "Pizza", 250, 5, null, null, null, 10, true, 1);
            var resGood = validator.TestValidate(goodCmd);
            resGood.ShouldNotHaveAnyValidationErrors();
        }
    }
}
