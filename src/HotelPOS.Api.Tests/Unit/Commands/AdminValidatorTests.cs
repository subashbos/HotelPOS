using Xunit;
using FluentValidation.TestHelper;
using HotelPOS.Application.DTOs.CashSession;
using HotelPOS.Application.DTOs.Table;
using HotelPOS.Application.UseCases.CashSessions.Commands;
using HotelPOS.Application.UseCases.Categories.Commands;
using HotelPOS.Application.UseCases.Roles.Commands;
using HotelPOS.Application.UseCases.Tables.Commands;
using HotelPOS.Application.UseCases.Users.Commands;

namespace HotelPOS.Tests.Unit.Commands
{
    /// <summary>
    /// Direct FluentValidation coverage for the Category/Table/Role/CashSession/User
    /// admin commands, which were previously only exercised incidentally through
    /// service- and integration-level tests.
    /// </summary>
    public class AdminValidatorTests
    {
        // ---------- CreateCategoryCommandValidator ----------
        [Fact]
        public void CreateCategoryCommandValidator_Validates_Correctly()
        {
            var validator = new CreateCategoryCommandValidator();

            var resBad = validator.TestValidate(new CreateCategoryCommand(""));
            resBad.ShouldHaveValidationErrorFor(x => x.Name);

            var resGood = validator.TestValidate(new CreateCategoryCommand("Beverages"));
            resGood.ShouldNotHaveAnyValidationErrors();
        }

        // ---------- UpdateCategoryCommandValidator ----------
        [Fact]
        public void UpdateCategoryCommandValidator_Validates_Correctly()
        {
            var validator = new UpdateCategoryCommandValidator();

            var resBad = validator.TestValidate(new UpdateCategoryCommand(0, ""));
            resBad.ShouldHaveValidationErrorFor(x => x.Id);
            resBad.ShouldHaveValidationErrorFor(x => x.Name);

            var resGood = validator.TestValidate(new UpdateCategoryCommand(1, "Beverages"));
            resGood.ShouldNotHaveAnyValidationErrors();
        }

        // ---------- DeleteCategoryCommandValidator ----------
        [Fact]
        public void DeleteCategoryCommandValidator_Validates_Correctly()
        {
            var validator = new DeleteCategoryCommandValidator();

            validator.TestValidate(new DeleteCategoryCommand(0)).ShouldHaveValidationErrorFor(x => x.Id);
            validator.TestValidate(new DeleteCategoryCommand(-1)).ShouldHaveValidationErrorFor(x => x.Id);
            validator.TestValidate(new DeleteCategoryCommand(1)).ShouldNotHaveAnyValidationErrors();
        }

        // ---------- CreateTableCommandValidator ----------
        [Fact]
        public void CreateTableCommandValidator_Validates_Correctly()
        {
            var validator = new CreateTableCommandValidator();

            var badDto = new CreateTableDto { Number = 0, Name = "T1", Capacity = 4 };
            validator.TestValidate(new CreateTableCommand(badDto)).ShouldHaveValidationErrorFor("Dto.Number");

            var goodDto = new CreateTableDto { Number = 5, Name = "T5", Capacity = 4 };
            validator.TestValidate(new CreateTableCommand(goodDto)).ShouldNotHaveAnyValidationErrors();
        }

        // ---------- UpdateTableCommandValidator ----------
        [Fact]
        public void UpdateTableCommandValidator_Validates_Correctly()
        {
            var validator = new UpdateTableCommandValidator();

            var badDto = new CreateTableDto { Number = 0, Name = "T1", Capacity = 4 };
            var resBad = validator.TestValidate(new UpdateTableCommand(0, badDto));
            resBad.ShouldHaveValidationErrorFor(x => x.Id);
            resBad.ShouldHaveValidationErrorFor("Dto.Number");

            var goodDto = new CreateTableDto { Number = 5, Name = "T5", Capacity = 4 };
            validator.TestValidate(new UpdateTableCommand(1, goodDto)).ShouldNotHaveAnyValidationErrors();
        }

        // ---------- DeleteTableCommandValidator ----------
        [Fact]
        public void DeleteTableCommandValidator_Validates_Correctly()
        {
            var validator = new DeleteTableCommandValidator();

            validator.TestValidate(new DeleteTableCommand(0)).ShouldHaveValidationErrorFor(x => x.Id);
            validator.TestValidate(new DeleteTableCommand(1)).ShouldNotHaveAnyValidationErrors();
        }

        // ---------- AddRoleCommandValidator ----------
        [Fact]
        public void AddRoleCommandValidator_Validates_Correctly()
        {
            var validator = new AddRoleCommandValidator();

            validator.TestValidate(new AddRoleCommand("", "desc")).ShouldHaveValidationErrorFor(x => x.Name);

            var tooLong = new string('A', 51);
            validator.TestValidate(new AddRoleCommand(tooLong, "desc")).ShouldHaveValidationErrorFor(x => x.Name);

            validator.TestValidate(new AddRoleCommand("Manager", "desc")).ShouldNotHaveAnyValidationErrors();
        }

        // ---------- DeleteRoleCommandValidator ----------
        [Fact]
        public void DeleteRoleCommandValidator_Validates_Correctly()
        {
            var validator = new DeleteRoleCommandValidator();

            validator.TestValidate(new DeleteRoleCommand(0)).ShouldHaveValidationErrorFor(x => x.Id);
            validator.TestValidate(new DeleteRoleCommand(1)).ShouldNotHaveAnyValidationErrors();
        }

        // ---------- OpenSessionCommandValidator ----------
        [Fact]
        public void OpenSessionCommandValidator_Validates_Correctly()
        {
            var validator = new OpenSessionCommandValidator();

            validator.TestValidate(new OpenSessionCommand(null!)).ShouldHaveValidationErrorFor(x => x.Dto);

            var badDto = new OpenSessionDto { OpeningBalance = -1, OpenedBy = "" };
            var resBad = validator.TestValidate(new OpenSessionCommand(badDto));
            resBad.ShouldHaveValidationErrorFor("Dto.OpeningBalance");
            resBad.ShouldHaveValidationErrorFor("Dto.OpenedBy");

            var goodDto = new OpenSessionDto { OpeningBalance = 500, OpenedBy = "cashier1" };
            validator.TestValidate(new OpenSessionCommand(goodDto)).ShouldNotHaveAnyValidationErrors();
        }

        // ---------- CloseSessionCommandValidator ----------
        [Fact]
        public void CloseSessionCommandValidator_Validates_Correctly()
        {
            var validator = new CloseSessionCommandValidator();

            validator.TestValidate(new CloseSessionCommand(null!)).ShouldHaveValidationErrorFor(x => x.Dto);

            var badDto = new CloseSessionDto { ActualCash = -1, ClosedBy = "" };
            var resBad = validator.TestValidate(new CloseSessionCommand(badDto));
            resBad.ShouldHaveValidationErrorFor("Dto.ActualCash");
            resBad.ShouldHaveValidationErrorFor("Dto.ClosedBy");

            var goodDto = new CloseSessionDto { ActualCash = 500, ClosedBy = "cashier1" };
            validator.TestValidate(new CloseSessionCommand(goodDto)).ShouldNotHaveAnyValidationErrors();
        }

        // ---------- ResetPasswordCommandValidator ----------
        [Theory]
        [InlineData(0, "GoodPass1!")]
        [InlineData(1, "")]
        [InlineData(1, "short1!")] // below minimum length
        [InlineData(1, "alllowercase1!")] // missing uppercase
        [InlineData(1, "ALLUPPERCASE1!")] // missing lowercase
        [InlineData(1, "NoDigitsHere!")] // missing digit
        [InlineData(1, "NoSpecialChars1")] // missing special character
        public void ResetPasswordCommandValidator_Rejects_Invalid_Input(int userId, string password)
        {
            var validator = new ResetPasswordCommandValidator();

            var result = validator.TestValidate(new ResetPasswordCommand(userId, password));

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ResetPasswordCommandValidator_Accepts_Valid_Input()
        {
            var validator = new ResetPasswordCommandValidator();

            var result = validator.TestValidate(new ResetPasswordCommand(1, "Str0ng!Pass"));

            result.ShouldNotHaveAnyValidationErrors();
        }

        // ---------- DeleteUserCommandValidator ----------
        [Fact]
        public void DeleteUserCommandValidator_Validates_Correctly()
        {
            var validator = new DeleteUserCommandValidator();

            validator.TestValidate(new DeleteUserCommand(0, 2)).ShouldHaveValidationErrorFor(x => x.UserId);
            validator.TestValidate(new DeleteUserCommand(1, 2)).ShouldNotHaveAnyValidationErrors();
        }
    }
}
