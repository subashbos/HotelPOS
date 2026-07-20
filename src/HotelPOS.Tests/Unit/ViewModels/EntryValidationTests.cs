using HotelPOS.ViewModels;
using Xunit;

namespace HotelPOS.Tests.Unit.ViewModels
{
    public class EntryValidationTests
    {
        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData("Valid", true)]
        public void ValidateRequired_ChecksForValue(string? value, bool expectedResult)
        {
            var result = EntryValidation.ValidateRequired(value, "TestField", out var error);
            Assert.Equal(expectedResult, result);
            if (!expectedResult)
            {
                Assert.Equal("TestField is required", error);
            }
            else
            {
                Assert.Empty(error);
            }
        }

        [Theory]
        [InlineData(null, true, "")]
        [InlineData("", true, "")]
        [InlineData("   ", true, "")]
        [InlineData("12345", false, "Invalid phone number (must be 10-15 digits)")]
        [InlineData("1234567890", true, "")]
        [InlineData("123-456-7890", true, "")]
        [InlineData("(123) 456-7890", true, "")]
        [InlineData("123456789012345", true, "")]
        [InlineData("1234567890123456", false, "Invalid phone number (must be 10-15 digits)")]
        public void ValidatePhone_ValidatesLengthAfterClean(string? phone, bool expectedResult, string expectedError)
        {
            var result = EntryValidation.ValidatePhone(phone, out var error);
            Assert.Equal(expectedResult, result);
            Assert.Equal(expectedError, error);
        }

        [Theory]
        [InlineData(null, true, "")]
        [InlineData("", true, "")]
        [InlineData("   ", true, "")]
        [InlineData("invalid-email", false, "Please enter a valid Email ID")]
        [InlineData("test@domain.com", true, "")]
        [InlineData("  test@domain.com  ", true, "")]
        public void ValidateEmail_ValidatesFormat(string? email, bool expectedResult, string expectedError)
        {
            var result = EntryValidation.ValidateEmail(email, out var error);
            Assert.Equal(expectedResult, result);
            Assert.Equal(expectedError, error);
        }
    }
}
