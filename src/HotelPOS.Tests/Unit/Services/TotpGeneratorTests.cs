using System.Reflection;
using System.Text;
using HotelPOS.Domain.Common;
using Xunit;

namespace HotelPOS.Tests
{
    public class TotpGeneratorTests
    {
        // RFC 6238 Appendix B test vector: ASCII secret "12345678901234567890",
        // HMAC-SHA1, at T=59s (step 1) the 8-digit code is 94287082 -> 6-digit truncation is 287082.
        [Fact]
        public void ComputeCode_MatchesRfc6238TestVector()
        {
            var secretBytes = Encoding.ASCII.GetBytes("12345678901234567890");
            var method = typeof(TotpGenerator).GetMethod("ComputeCode", BindingFlags.NonPublic | BindingFlags.Static)!;

            var code = (string)method.Invoke(null, new object[] { secretBytes, 1L })!;

            Assert.Equal("287082", code);
        }

        [Fact]
        public void GenerateSecret_ProducesValidBase32ThatRoundTripsThroughValidateCode()
        {
            var secret = TotpGenerator.GenerateSecret();
            Assert.False(string.IsNullOrWhiteSpace(secret));

            // Compute the code for the current step directly (mirrors ValidateCode's internal step calc)
            var stepMethod = typeof(TotpGenerator).GetMethod("GetCurrentStep", BindingFlags.NonPublic | BindingFlags.Static)!;
            var computeMethod = typeof(TotpGenerator).GetMethod("ComputeCode", BindingFlags.NonPublic | BindingFlags.Static)!;
            var decodeMethod = typeof(TotpGenerator).GetMethod("Base32Decode", BindingFlags.NonPublic | BindingFlags.Static)!;

            var step = (long)stepMethod.Invoke(null, null)!;
            var secretBytes = (byte[])decodeMethod.Invoke(null, new object[] { secret })!;
            var expectedCode = (string)computeMethod.Invoke(null, new object[] { secretBytes, step })!;

            Assert.True(TotpGenerator.ValidateCode(secret, expectedCode));
        }

        [Fact]
        public void ValidateCode_WrongCode_ReturnsFalse()
        {
            var secret = TotpGenerator.GenerateSecret();
            Assert.False(TotpGenerator.ValidateCode(secret, "000000") && TotpGenerator.ValidateCode(secret, "999999"));
        }

        [Fact]
        public void ValidateCode_MalformedInput_ReturnsFalseWithoutThrowing()
        {
            Assert.False(TotpGenerator.ValidateCode("not-valid-base32!!!", "123456"));
            Assert.False(TotpGenerator.ValidateCode("", "123456"));
            Assert.False(TotpGenerator.ValidateCode(null, "123456"));
            Assert.False(TotpGenerator.ValidateCode("ABCDEFGH", "12"));
            Assert.False(TotpGenerator.ValidateCode("ABCDEFGH", "abcdef"));
        }
    }
}
