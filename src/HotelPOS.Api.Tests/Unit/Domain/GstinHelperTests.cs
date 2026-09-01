using HotelPOS.Domain.Common;
using Xunit;

namespace HotelPOS.Tests.Unit.Domain
{
    public class GstinHelperTests
    {
        [Theory]
        [InlineData("33AQZPS2365E1ZE", "33")]
        [InlineData("29APWAS2365E1ZE", "29")]
        public void DeriveStateCode_ReturnsFirstTwoDigitsOfGstin(string gstin, string expected)
        {
            Assert.Equal(expected, GstinHelper.DeriveStateCode(gstin));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("3")]
        public void DeriveStateCode_MissingOrTooShortGstin_ReturnsEmptyString(string? gstin)
        {
            Assert.Equal(string.Empty, GstinHelper.DeriveStateCode(gstin));
        }

        [Fact]
        public void IsInterstate_DifferentStateCodes_ReturnsTrue()
        {
            Assert.True(GstinHelper.IsInterstate("33AQZPS2365E1ZE", "29APWAS2365E1ZE"));
        }

        [Fact]
        public void IsInterstate_SameStateCode_ReturnsFalse()
        {
            Assert.False(GstinHelper.IsInterstate("33AQZPS2365E1ZE", "33APWAS2365E1ZE"));
        }

        [Fact]
        public void IsInterstate_NoCustomerGstin_ReturnsFalse()
        {
            // B2C sale, no GSTIN on file - safe default is intrastate.
            Assert.False(GstinHelper.IsInterstate("33AQZPS2365E1ZE", null));
        }

        [Fact]
        public void IsInterstate_NoHotelGstinConfigured_ReturnsFalse()
        {
            // Settings never populated - can't determine the hotel's own state, so never claim interstate.
            Assert.False(GstinHelper.IsInterstate(null, "29APWAS2365E1ZE"));
        }
    }
}
