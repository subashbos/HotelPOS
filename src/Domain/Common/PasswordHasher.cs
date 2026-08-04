using System;
using System.Globalization;
using System.Security.Cryptography;
using HotelPOS.Domain.Common.Constants;

namespace HotelPOS.Domain.Common
{
    /// <summary>
    /// Central PBKDF2-SHA256 password hashing, used by every login/reset/create-user path.
    /// </summary>
    /// <remarks>
    /// The iteration count is embedded in the stored hash as "&lt;iterations&gt;:&lt;base64hash&gt;",
    /// so raising <see cref="ValidationLimits.Pbkdf2Iterations"/> only affects newly-written hashes -
    /// existing rows keep verifying at whatever count they were actually hashed with. Rows written
    /// before this encoding existed are a bare base64 hash with no ':' separator; those are treated
    /// as having been hashed at <see cref="ValidationLimits.Pbkdf2LegacyIterations"/>. Callers on the
    /// login path should check <see cref="NeedsRehash"/> after a successful verify and, if true,
    /// persist a fresh <see cref="Hash"/> of the just-verified password to transparently upgrade it.
    /// </remarks>
    public static class PasswordHasher
    {
        private const char Separator = ':';

        public static (string Hash, string Salt) Hash(string password)
        {
            var saltBytes = RandomNumberGenerator.GetBytes(ValidationLimits.SaltByteSize);
            var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
                password, saltBytes, ValidationLimits.Pbkdf2Iterations, HashAlgorithmName.SHA256, ValidationLimits.HashByteSize);

            var encodedHash = ValidationLimits.Pbkdf2Iterations.ToString(CultureInfo.InvariantCulture) + Separator + Convert.ToBase64String(hashBytes);
            return (encodedHash, Convert.ToBase64String(saltBytes));
        }

        public static bool Verify(string password, string storedHash, string storedSalt)
        {
            var (iterations, expectedHash) = ParseStoredHash(storedHash);
            var saltBytes = Convert.FromBase64String(storedSalt);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, iterations, HashAlgorithmName.SHA256, expectedHash.Length);

            return actualHash.Length == expectedHash.Length && CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }

        /// <summary>True if <paramref name="storedHash"/> was hashed at fewer iterations than the
        /// current standard and should be re-hashed and persisted next time it's successfully verified.</summary>
        public static bool NeedsRehash(string storedHash)
        {
            var (iterations, _) = ParseStoredHash(storedHash);
            return iterations < ValidationLimits.Pbkdf2Iterations;
        }

        private static (int Iterations, byte[] Hash) ParseStoredHash(string storedHash)
        {
            var separatorIndex = storedHash.IndexOf(Separator);
            if (separatorIndex < 0)
                return (ValidationLimits.Pbkdf2LegacyIterations, Convert.FromBase64String(storedHash));

            var iterations = int.Parse(storedHash.AsSpan(0, separatorIndex), NumberStyles.None, CultureInfo.InvariantCulture);
            var hash = Convert.FromBase64String(storedHash.Substring(separatorIndex + 1));
            return (iterations, hash);
        }
    }
}
