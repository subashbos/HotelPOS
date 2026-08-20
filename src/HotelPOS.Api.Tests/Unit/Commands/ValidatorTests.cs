using Xunit;
using FluentValidation.TestHelper;
using HotelPOS.Application.UseCases.Suppliers.Commands;
using HotelPOS.Application.UseCases.Expenses.Commands;
using HotelPOS.Application.UseCases.Purchases.Commands;
using HotelPOS.Application.UseCases.Users.Commands;
using HotelPOS.Application.UseCases.Settings.Commands;
using HotelPOS.Application.UseCases.Items.Commands;
using HotelPOS.Application.UseCases.Auth.Commands;
using HotelPOS.Application.UseCases.UnitOfMeasurements.Commands;
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

        // ---------- LoginCommandValidator ----------
        [Fact]
        public void LoginCommandValidator_Validates_Correctly()
        {
            var validator = new LoginCommandValidator();

            var badCmd = new LoginCommand("", "");
            var resBad = validator.TestValidate(badCmd);
            resBad.ShouldHaveValidationErrorFor(x => x.Username);
            resBad.ShouldHaveValidationErrorFor(x => x.Password);

            var goodCmd = new LoginCommand("admin", "Sup3rSecret!x");
            var resGood = validator.TestValidate(goodCmd);
            resGood.ShouldNotHaveAnyValidationErrors();
        }

        // ---------- CreateUnitOfMeasurementCommandValidator ----------
        [Fact]
        public void CreateUnitOfMeasurementCommandValidator_Validates_Correctly()
        {
            var validator = new CreateUnitOfMeasurementCommandValidator();

            var resBad = validator.TestValidate(new CreateUnitOfMeasurementCommand(""));
            resBad.ShouldHaveValidationErrorFor(x => x.Name);

            var resGood = validator.TestValidate(new CreateUnitOfMeasurementCommand("Kilogram"));
            resGood.ShouldNotHaveAnyValidationErrors();
        }

        // ---------- UpdateUnitOfMeasurementCommandValidator ----------
        [Fact]
        public void UpdateUnitOfMeasurementCommandValidator_Validates_Correctly()
        {
            var validator = new UpdateUnitOfMeasurementCommandValidator();

            var resBad = validator.TestValidate(new UpdateUnitOfMeasurementCommand(0, ""));
            resBad.ShouldHaveValidationErrorFor(x => x.Id);
            resBad.ShouldHaveValidationErrorFor(x => x.Name);

            var resGood = validator.TestValidate(new UpdateUnitOfMeasurementCommand(1, "Kilogram"));
            resGood.ShouldNotHaveAnyValidationErrors();
        }

        // ---------- DeleteUnitOfMeasurementCommandValidator ----------
        [Fact]
        public void DeleteUnitOfMeasurementCommandValidator_Validates_Correctly()
        {
            var validator = new DeleteUnitOfMeasurementCommandValidator();

            var resBad = validator.TestValidate(new DeleteUnitOfMeasurementCommand(0));
            resBad.ShouldHaveValidationErrorFor(x => x.Id);

            var resGood = validator.TestValidate(new DeleteUnitOfMeasurementCommand(1));
            resGood.ShouldNotHaveAnyValidationErrors();
        }

        // ---------- DeleteSupplierCommandValidator ----------
        [Fact]
        public void DeleteSupplierCommandValidator_Validates_Correctly()
        {
            var validator = new DeleteSupplierCommandValidator();

            var resBad = validator.TestValidate(new DeleteSupplierCommand(0));
            resBad.ShouldHaveValidationErrorFor(x => x.Id);

            var resGood = validator.TestValidate(new DeleteSupplierCommand(1));
            resGood.ShouldNotHaveAnyValidationErrors();
        }

        // ---------- CreateItemCommandValidator ----------
        [Fact]
        public void CreateItemCommandValidator_Validates_Correctly()
        {
            var validator = new CreateItemCommandValidator();

            var badCmd = new CreateItemCommand("", -5, -1, null, null, null, 0, false, 0);
            var resBad = validator.TestValidate(badCmd);
            resBad.ShouldHaveValidationErrorFor(x => x.Name);
            resBad.ShouldHaveValidationErrorFor(x => x.Price);
            resBad.ShouldHaveValidationErrorFor(x => x.TaxPercentage);
            resBad.ShouldHaveValidationErrorFor(x => x.UnitId);

            var goodCmd = new CreateItemCommand("Pizza", 250, 5, null, null, null, 10, true, 1);
            var resGood = validator.TestValidate(goodCmd);
            resGood.ShouldNotHaveAnyValidationErrors();
        }

        // ---------- DeleteItemCommandValidator ----------
        [Fact]
        public void DeleteItemCommandValidator_Validates_Correctly()
        {
            var validator = new DeleteItemCommandValidator();

            var resBad = validator.TestValidate(new DeleteItemCommand(0));
            resBad.ShouldHaveValidationErrorFor(x => x.Id);

            var resGood = validator.TestValidate(new DeleteItemCommand(1));
            resGood.ShouldNotHaveAnyValidationErrors();
        }

        // ---------- DeleteExpenseCommandValidator ----------
        [Fact]
        public void DeleteExpenseCommandValidator_Validates_Correctly()
        {
            var validator = new DeleteExpenseCommandValidator();

            var resBad = validator.TestValidate(new DeleteExpenseCommand(0));
            resBad.ShouldHaveValidationErrorFor(x => x.Id);

            var resGood = validator.TestValidate(new DeleteExpenseCommand(1));
            resGood.ShouldNotHaveAnyValidationErrors();
        }

        // ---------- UpdatePurchaseCommandValidator ----------
        [Fact]
        public void UpdatePurchaseCommandValidator_Validates_Correctly()
        {
            var validator = new UpdatePurchaseCommandValidator();

            var resNull = validator.TestValidate(new UpdatePurchaseCommand(null!));
            resNull.ShouldHaveValidationErrorFor(x => x.Purchase);

            var badPurchase = new Purchase
            {
                Id = 0,
                SupplierId = 0,
                GrandTotal = -10,
                PurchaseItems = new List<PurchaseItem>
                {
                    new PurchaseItem { Quantity = 0, UnitPrice = -5 }
                }
            };
            var resBad = validator.TestValidate(new UpdatePurchaseCommand(badPurchase));
            resBad.ShouldHaveValidationErrorFor(x => x.Purchase.Id);
            resBad.ShouldHaveValidationErrorFor(x => x.Purchase.SupplierId);
            resBad.ShouldHaveValidationErrorFor(x => x.Purchase.GrandTotal);
            resBad.ShouldHaveValidationErrorFor("Purchase.PurchaseItems[0].Quantity");
            resBad.ShouldHaveValidationErrorFor("Purchase.PurchaseItems[0].UnitPrice");

            var goodPurchase = new Purchase
            {
                Id = 5,
                SupplierId = 5,
                GrandTotal = 1500,
                PurchaseDate = DateTime.Today,
                PurchaseItems = new List<PurchaseItem>
                {
                    new PurchaseItem { ItemId = 1, Quantity = 10, UnitPrice = 150 }
                }
            };
            var resGood = validator.TestValidate(new UpdatePurchaseCommand(goodPurchase));
            resGood.ShouldNotHaveAnyValidationErrors();
        }

        // ---------- CustomerValidator ----------
        [Fact]
        public void CustomerValidator_Validates_Correctly()
        {
            var validator = new CustomerValidator();

            // Invalid fields
            var badCustomer = new Customer
            {
                Name = "",
                Phone = "123", // too few digits
                Email = "not-an-email",
                Gstin = "invalid_gst"
            };
            var resBad = validator.TestValidate(badCustomer);
            resBad.ShouldHaveValidationErrorFor(x => x.Name);
            resBad.ShouldHaveValidationErrorFor(x => x.Phone);
            resBad.ShouldHaveValidationErrorFor(x => x.Email);
            resBad.ShouldHaveValidationErrorFor(x => x.Gstin);

            // Valid fields
            var goodCustomer = new Customer
            {
                Name = "Walk-in Customer",
                Phone = "9876543210",
                Email = "customer@test.com",
                Gstin = "27AABCU9603R1ZX" // Valid Indian GSTIN format
            };
            var resGood = validator.TestValidate(goodCustomer);
            resGood.ShouldNotHaveAnyValidationErrors();
        }

        // ---------- EmployeeValidator ----------
        [Fact]
        public void EmployeeValidator_Validates_Correctly()
        {
            var validator = new EmployeeValidator();

            // Invalid fields
            var badEmployee = new Employee
            {
                EmployeeCode = "",
                FirstName = "",
                DateOfJoining = default(DateTime),
                Phone = "12345", // too few digits
                Email = "bad-email",
                Pan = "ABCD12345E", // 5th char must be a letter, not a digit
                Aadhaar = "12345", // not 12 digits
                BankIfsc = "SBIN1001234" // 5th char must be literal '0'
            };
            var resBad = validator.TestValidate(badEmployee);
            resBad.ShouldHaveValidationErrorFor(x => x.EmployeeCode);
            resBad.ShouldHaveValidationErrorFor(x => x.FirstName);
            resBad.ShouldHaveValidationErrorFor(x => x.DateOfJoining);
            resBad.ShouldHaveValidationErrorFor(x => x.Phone);
            resBad.ShouldHaveValidationErrorFor(x => x.Email);
            resBad.ShouldHaveValidationErrorFor(x => x.Pan);
            resBad.ShouldHaveValidationErrorFor(x => x.Aadhaar);
            resBad.ShouldHaveValidationErrorFor(x => x.BankIfsc);

            // Valid fields
            var goodEmployee = new Employee
            {
                EmployeeCode = "EMP001",
                FirstName = "Ravi",
                DateOfJoining = new DateTime(2024, 1, 1),
                Phone = "9876543210",
                Email = "ravi@test.com",
                Pan = "ABCDE1234F",
                Aadhaar = "234567890123",
                BankIfsc = "SBIN0001234"
            };
            var resGood = validator.TestValidate(goodEmployee);
            resGood.ShouldNotHaveAnyValidationErrors();
        }

        // ---------- EmployeeValidator: DateOfExit boundary ----------
        [Fact]
        public void EmployeeValidator_DateOfExitBeforeDateOfJoining_HasError()
        {
            var validator = new EmployeeValidator();

            var badEmployee = new Employee
            {
                EmployeeCode = "EMP002",
                FirstName = "Asha",
                DateOfJoining = new DateTime(2024, 6, 1),
                DateOfExit = new DateTime(2024, 5, 1) // before DateOfJoining
            };
            var resBad = validator.TestValidate(badEmployee);
            resBad.ShouldHaveValidationErrorFor(x => x.DateOfExit);

            var goodEmployee = new Employee
            {
                EmployeeCode = "EMP002",
                FirstName = "Asha",
                DateOfJoining = new DateTime(2024, 6, 1),
                DateOfExit = new DateTime(2024, 6, 1) // equal is allowed (>=)
            };
            var resGood = validator.TestValidate(goodEmployee);
            resGood.ShouldNotHaveValidationErrorFor(x => x.DateOfExit);
        }

        // ---------- SalaryStructureValidator ----------
        [Fact]
        public void SalaryStructureValidator_Validates_Correctly()
        {
            var validator = new SalaryStructureValidator();

            // Invalid fields
            var badSalary = new SalaryStructure
            {
                EmployeeId = 0,
                Basic = 0,
                Hra = -1,
                Da = -1,
                ConveyanceAllowance = -1,
                MedicalAllowance = -1,
                SpecialAllowance = -1,
                EffectiveFrom = new DateTime(2024, 6, 1),
                EffectiveTo = new DateTime(2024, 5, 1) // before EffectiveFrom
            };
            var resBad = validator.TestValidate(badSalary);
            resBad.ShouldHaveValidationErrorFor(x => x.EmployeeId);
            resBad.ShouldHaveValidationErrorFor(x => x.Basic);
            resBad.ShouldHaveValidationErrorFor(x => x.Hra);
            resBad.ShouldHaveValidationErrorFor(x => x.Da);
            resBad.ShouldHaveValidationErrorFor(x => x.ConveyanceAllowance);
            resBad.ShouldHaveValidationErrorFor(x => x.MedicalAllowance);
            resBad.ShouldHaveValidationErrorFor(x => x.SpecialAllowance);
            resBad.ShouldHaveValidationErrorFor(x => x.EffectiveTo);

            // Valid fields
            var goodSalary = new SalaryStructure
            {
                EmployeeId = 1,
                Basic = 15000,
                Hra = 5000,
                Da = 2000,
                ConveyanceAllowance = 1000,
                MedicalAllowance = 1000,
                SpecialAllowance = 500,
                EffectiveFrom = new DateTime(2024, 1, 1),
                EffectiveTo = new DateTime(2024, 12, 31)
            };
            var resGood = validator.TestValidate(goodSalary);
            resGood.ShouldNotHaveAnyValidationErrors();
        }

        // ---------- SalaryStructureValidator: EffectiveTo boundary ----------
        [Fact]
        public void SalaryStructureValidator_EffectiveToEqualsEffectiveFrom_IsValid()
        {
            var validator = new SalaryStructureValidator();

            var salary = new SalaryStructure
            {
                EmployeeId = 1,
                Basic = 15000,
                EffectiveFrom = new DateTime(2024, 1, 1),
                EffectiveTo = new DateTime(2024, 1, 1) // equal is allowed (>=)
            };
            var res = validator.TestValidate(salary);
            res.ShouldNotHaveValidationErrorFor(x => x.EffectiveTo);
        }

        // ---------- LeaveRequestValidator ----------
        [Fact]
        public void LeaveRequestValidator_Validates_Correctly()
        {
            var validator = new LeaveRequestValidator();

            // Invalid fields
            var badLeave = new LeaveRequest
            {
                EmployeeId = 0,
                LeaveTypeId = 0,
                FromDate = new DateTime(2024, 6, 10),
                ToDate = new DateTime(2024, 6, 9), // before FromDate
                TotalDays = 0
            };
            var resBad = validator.TestValidate(badLeave);
            resBad.ShouldHaveValidationErrorFor(x => x.EmployeeId);
            resBad.ShouldHaveValidationErrorFor(x => x.LeaveTypeId);
            resBad.ShouldHaveValidationErrorFor(x => x.ToDate);
            resBad.ShouldHaveValidationErrorFor(x => x.TotalDays);

            // Valid fields
            var goodLeave = new LeaveRequest
            {
                EmployeeId = 1,
                LeaveTypeId = 1,
                FromDate = new DateTime(2024, 6, 10),
                ToDate = new DateTime(2024, 6, 12),
                TotalDays = 3
            };
            var resGood = validator.TestValidate(goodLeave);
            resGood.ShouldNotHaveAnyValidationErrors();
        }

        // ---------- LeaveRequestValidator: ToDate boundary ----------
        [Fact]
        public void LeaveRequestValidator_ToDateEqualsFromDate_IsValid()
        {
            var validator = new LeaveRequestValidator();

            var leave = new LeaveRequest
            {
                EmployeeId = 1,
                LeaveTypeId = 1,
                FromDate = new DateTime(2024, 6, 10),
                ToDate = new DateTime(2024, 6, 10), // single-day leave, equal is allowed (>=)
                TotalDays = 1
            };
            var res = validator.TestValidate(leave);
            res.ShouldNotHaveAnyValidationErrors();
        }

        // ---------- PurchaseValidator ----------
        [Fact]
        public void PurchaseValidator_Validates_Correctly()
        {
            var validator = new PurchaseValidator();

            // Invalid fields
            var badPurchase = new Purchase
            {
                SupplierId = 0,
                InvoiceNumber = "",
                PurchaseItems = new List<PurchaseItem>
                {
                    new PurchaseItem { ItemId = 0, ItemName = "Rice", Quantity = 0, UnitPrice = -5 }
                }
            };
            var resBad = validator.TestValidate(badPurchase);
            resBad.ShouldHaveValidationErrorFor(x => x.SupplierId);
            resBad.ShouldHaveValidationErrorFor(x => x.InvoiceNumber);
            resBad.ShouldHaveValidationErrorFor("PurchaseItems[0].ItemId");
            resBad.ShouldHaveValidationErrorFor("PurchaseItems[0].Quantity");
            resBad.ShouldHaveValidationErrorFor("PurchaseItems[0].UnitPrice");

            // Valid fields
            var goodPurchase = new Purchase
            {
                SupplierId = 5,
                InvoiceNumber = "INV-001",
                PurchaseDate = DateTime.Today,
                PurchaseItems = new List<PurchaseItem>
                {
                    new PurchaseItem { ItemId = 1, ItemName = "Rice", Quantity = 10, UnitPrice = 150 }
                }
            };
            var resGood = validator.TestValidate(goodPurchase);
            resGood.ShouldNotHaveAnyValidationErrors();
        }

        // ---------- PurchaseValidator: items boundary ----------
        [Fact]
        public void PurchaseValidator_EmptyItems_HasError()
        {
            var validator = new PurchaseValidator();

            var purchase = new Purchase
            {
                SupplierId = 1,
                InvoiceNumber = "INV-002",
                PurchaseItems = new List<PurchaseItem>()
            };
            var res = validator.TestValidate(purchase);
            res.ShouldHaveValidationErrorFor(x => x.PurchaseItems);
        }

        // ---------- PurchaseValidator: unit price boundary ----------
        [Fact]
        public void PurchaseValidator_ZeroUnitPrice_IsValid()
        {
            var validator = new PurchaseValidator();

            var purchase = new Purchase
            {
                SupplierId = 1,
                InvoiceNumber = "INV-003",
                PurchaseItems = new List<PurchaseItem>
                {
                    new PurchaseItem { ItemId = 1, ItemName = "Free Sample", Quantity = 1, UnitPrice = 0 }
                }
            };
            var res = validator.TestValidate(purchase);
            res.ShouldNotHaveAnyValidationErrors();
        }
    }
}
