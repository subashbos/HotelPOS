using System.Security.Cryptography;
using HotelPOS.Infrastructure.Persistence;
using Xunit;

namespace HotelPOS.Tests.Unit.Services
{
    /// <summary>
    /// Uses the converter's fixed-key constructor throughout so these tests are isolated from the
    /// shared, process-wide <see cref="PiiEncryptionKeyProvider"/> static and can't race with API
    /// integration tests (CustomWebApplicationFactory) that set the ambient key concurrently.
    /// </summary>
    public class EncryptedStringConverterTests
    {
        private static byte[] NewKey() => RandomNumberGenerator.GetBytes(32);

        private static string? RoundTripEncrypt(EncryptedStringConverter converter, string? value) =>
            (string?)converter.ConvertToProvider(value);

        private static string? RoundTripDecrypt(EncryptedStringConverter converter, string? value) =>
            (string?)converter.ConvertFromProvider(value);

        [Fact]
        public void RoundTrips_ThroughEncryptThenDecrypt()
        {
            var converter = new EncryptedStringConverter(NewKey());

            var stored = RoundTripEncrypt(converter, "ABCDE1234F");
            var recovered = RoundTripDecrypt(converter, stored);

            Assert.NotEqual("ABCDE1234F", stored);
            Assert.StartsWith("ENCv1:", stored);
            Assert.Equal("ABCDE1234F", recovered);
        }

        [Fact]
        public void Encrypt_SamePlaintextTwice_ProducesDifferentCiphertext()
        {
            var converter = new EncryptedStringConverter(NewKey());

            var first = RoundTripEncrypt(converter, "999988887777");
            var second = RoundTripEncrypt(converter, "999988887777");

            Assert.NotEqual(first, second); // random nonce per encryption
        }

        [Fact]
        public void Decrypt_LegacyPlaintextWithoutPrefix_PassesThroughUnchanged()
        {
            var converter = new EncryptedStringConverter(NewKey());

            var result = RoundTripDecrypt(converter, "ABCDE1234F");

            Assert.Equal("ABCDE1234F", result);
        }

        [Fact]
        public void Decrypt_TamperedCiphertext_Throws()
        {
            var converter = new EncryptedStringConverter(NewKey());
            var stored = RoundTripEncrypt(converter, "ABCDE1234F")!;
            var tampered = stored[..^1] + (stored[^1] == 'A' ? 'B' : 'A');

            Assert.ThrowsAny<CryptographicException>(() => RoundTripDecrypt(converter, tampered));
        }

        [Fact]
        public void NoKeyConfigured_EncryptIsPassthrough()
        {
            var converter = new EncryptedStringConverter(fixedKey: null);

            var result = RoundTripEncrypt(converter, "ABCDE1234F");

            Assert.Equal("ABCDE1234F", result);
        }

        [Fact]
        public void NoKeyConfigured_DecryptOfLegacyPlaintextIsPassthrough()
        {
            var converter = new EncryptedStringConverter(fixedKey: null);

            var result = RoundTripDecrypt(converter, "ABCDE1234F");

            Assert.Equal("ABCDE1234F", result);
        }

        [Fact]
        public void NoKeyConfigured_DecryptOfEncryptedValue_Throws()
        {
            var withKey = new EncryptedStringConverter(NewKey());
            var stored = RoundTripEncrypt(withKey, "ABCDE1234F");

            var withoutKey = new EncryptedStringConverter(fixedKey: null);

            Assert.Throws<InvalidOperationException>(() => RoundTripDecrypt(withoutKey, stored));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void EmptyOrNull_PassesThroughBothDirections(string? value)
        {
            var converter = new EncryptedStringConverter(NewKey());

            Assert.Equal(value, RoundTripEncrypt(converter, value));
            Assert.Equal(value, RoundTripDecrypt(converter, value));
        }

        [Fact]
        public void SetKey_RejectsWrongLength()
        {
            Assert.Throws<ArgumentException>(() => PiiEncryptionKeyProvider.SetKey(new byte[16]));
        }
    }
}
