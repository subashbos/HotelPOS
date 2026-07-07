using System.Security.Cryptography;
using System.Text;

namespace HotelPOS.Domain.Common
{
    /// <summary>RFC 6238 TOTP (the algorithm used by Google Authenticator / Microsoft Authenticator / Authy).</summary>
    public static class TotpGenerator
    {
        private const int SecretBytes = 20; // 160-bit secret
        private const int Digits = 6;
        private const int StepSeconds = 30;
        private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        public static string GenerateSecret()
        {
            return Base32Encode(RandomNumberGenerator.GetBytes(SecretBytes));
        }

        /// <summary>Builds the otpauth:// URI an authenticator app can import (via QR or manual add).</summary>
        public static string BuildOtpAuthUri(string secret, string username, string issuer = "HotelPOS")
        {
            var label = Uri.EscapeDataString($"{issuer}:{username}");
            var issuerParam = Uri.EscapeDataString(issuer);
            return $"otpauth://totp/{label}?secret={secret}&issuer={issuerParam}&digits={Digits}&period={StepSeconds}";
        }

        /// <summary>Validates a 6-digit code, tolerating +/- one 30s step for clock drift.</summary>
        public static bool ValidateCode(string? base32Secret, string? code, int windowSteps = 1)
        {
            if (string.IsNullOrWhiteSpace(base32Secret) || string.IsNullOrWhiteSpace(code)) return false;

            code = code.Trim();
            if (code.Length != Digits || !code.All(char.IsDigit)) return false;

            byte[] secretBytes;
            try
            {
                secretBytes = Base32Decode(base32Secret);
            }
            catch (FormatException)
            {
                return false;
            }

            var currentStep = GetCurrentStep();
            for (int i = -windowSteps; i <= windowSteps; i++)
            {
                if (ComputeCode(secretBytes, currentStep + i) == code) return true;
            }

            return false;
        }

        private static long GetCurrentStep() =>
            DateTimeOffset.UtcNow.ToUnixTimeSeconds() / StepSeconds;

        private static string ComputeCode(byte[] secret, long step)
        {
            var stepBytes = BitConverter.GetBytes(step);
            if (BitConverter.IsLittleEndian) Array.Reverse(stepBytes);

            using var hmac = new HMACSHA1(secret);
            var hash = hmac.ComputeHash(stepBytes);

            int offset = hash[^1] & 0x0F;
            int binaryCode = ((hash[offset] & 0x7F) << 24)
                            | ((hash[offset + 1] & 0xFF) << 16)
                            | ((hash[offset + 2] & 0xFF) << 8)
                            | (hash[offset + 3] & 0xFF);

            int otp = binaryCode % (int)Math.Pow(10, Digits);
            return otp.ToString().PadLeft(Digits, '0');
        }

        private static string Base32Encode(byte[] data)
        {
            var sb = new StringBuilder();
            int bits = 0, value = 0;
            foreach (var b in data)
            {
                value = (value << 8) | b;
                bits += 8;
                while (bits >= 5)
                {
                    sb.Append(Base32Alphabet[(value >> (bits - 5)) & 0x1F]);
                    bits -= 5;
                }
            }
            if (bits > 0)
            {
                sb.Append(Base32Alphabet[(value << (5 - bits)) & 0x1F]);
            }
            return sb.ToString();
        }

        private static byte[] Base32Decode(string input)
        {
            input = input.Trim().TrimEnd('=').ToUpperInvariant();
            var bytes = new List<byte>();
            int bits = 0, value = 0;
            foreach (var c in input)
            {
                int idx = Base32Alphabet.IndexOf(c);
                if (idx < 0) throw new FormatException("Invalid Base32 character.");
                value = (value << 5) | idx;
                bits += 5;
                if (bits >= 8)
                {
                    bytes.Add((byte)((value >> (bits - 8)) & 0xFF));
                    bits -= 8;
                }
            }
            return bytes.ToArray();
        }
    }
}
