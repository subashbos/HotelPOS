using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HotelPOS.Infrastructure.Persistence
{
    /// <summary>
    /// Holds the AES-256 key used to encrypt PII columns (Employee.Pan/Aadhaar/Uan/EsicNumber/
    /// BankAccountNumber/BankIfsc) at rest. Set once at host startup (API Program.cs / WPF
    /// App.xaml.cs) from the Encryption:PiiKey config value.
    /// </summary>
    /// <remarks>
    /// Deliberately NOT injected into HotelDbContext's constructor: several test files construct
    /// <c>new HotelDbContext(options)</c> directly, and <see cref="EncryptedStringConverter"/>
    /// reads the key lazily at actual read/write time rather than at model-building time, so those
    /// tests keep working untouched — encryption is a pure no-op when no key has been set.
    /// </remarks>
    public static class PiiEncryptionKeyProvider
    {
        private static byte[]? _key;

        public static void SetKey(byte[] key)
        {
            if (key == null || key.Length != 32)
                throw new ArgumentException("PII encryption key must be exactly 32 bytes (AES-256).", nameof(key));
            _key = key;
        }

        internal static bool TryGetKey(out byte[]? key)
        {
            key = _key;
            return _key != null;
        }
    }

    /// <summary>
    /// Transparent AES-256-GCM encryption for a single nvarchar column. Ciphertext is stored as
    /// <c>"ENCv1:" + Base64(nonce[12] + ciphertext + tag[16])</c>.
    /// </summary>
    /// <remarks>
    /// Values that predate encryption (no <c>ENCv1:</c> prefix) are tolerated as legacy plaintext
    /// on read and get encrypted the next time the owning row is saved — a lazy migration, since a
    /// bulk data migration isn't safely doable from a schema-only EF migration (it needs the
    /// runtime key and application code, not just SQL). A value that DOES carry the prefix but
    /// fails to decrypt is treated as tampering, not legacy data, and throws.
    /// </remarks>
    public sealed class EncryptedStringConverter : ValueConverter<string?, string?>
    {
        private const string Prefix = "ENCv1:";
        private const int NonceSize = 12;
        private const int TagSize = 16;

        /// <summary>Production use — reads the key from <see cref="PiiEncryptionKeyProvider"/> at each conversion.</summary>
        public EncryptedStringConverter()
            : this(useAmbientKey: true, fixedKey: null)
        {
        }

        /// <summary>
        /// Uses a fixed key instead of the ambient <see cref="PiiEncryptionKeyProvider"/> — for
        /// tests, so they're isolated from that shared, process-wide static and can't race with
        /// production code (e.g. API integration tests) mutating it concurrently. Pass
        /// <see langword="null"/> to force "no key configured" behavior regardless of ambient state.
        /// </summary>
        public EncryptedStringConverter(byte[]? fixedKey)
            : this(useAmbientKey: false, fixedKey: fixedKey)
        {
        }

        private EncryptedStringConverter(bool useAmbientKey, byte[]? fixedKey)
            : base(v => Encrypt(v, useAmbientKey, fixedKey), v => Decrypt(v, useAmbientKey, fixedKey))
        {
        }

        private static byte[]? ResolveKey(bool useAmbientKey, byte[]? fixedKey)
        {
            if (!useAmbientKey) return fixedKey;
            return PiiEncryptionKeyProvider.TryGetKey(out var key) ? key : null;
        }

        private static string? Encrypt(string? plaintext, bool useAmbientKey, byte[]? fixedKey)
        {
            if (string.IsNullOrEmpty(plaintext)) return plaintext;
            var key = ResolveKey(useAmbientKey, fixedKey);
            if (key == null) return plaintext;

            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            var plainBytes = Encoding.UTF8.GetBytes(plaintext);
            var cipherBytes = new byte[plainBytes.Length];
            var tag = new byte[TagSize];

            using (var aes = new AesGcm(key, TagSize))
            {
                aes.Encrypt(nonce, plainBytes, cipherBytes, tag);
            }

            var payload = new byte[NonceSize + cipherBytes.Length + TagSize];
            Buffer.BlockCopy(nonce, 0, payload, 0, NonceSize);
            Buffer.BlockCopy(cipherBytes, 0, payload, NonceSize, cipherBytes.Length);
            Buffer.BlockCopy(tag, 0, payload, NonceSize + cipherBytes.Length, TagSize);

            return Prefix + Convert.ToBase64String(payload);
        }

        private static string? Decrypt(string? stored, bool useAmbientKey, byte[]? fixedKey)
        {
            if (string.IsNullOrEmpty(stored) || !stored.StartsWith(Prefix, StringComparison.Ordinal))
                return stored; // legacy plaintext (or null/empty) — pass through unchanged

            var payload = Convert.FromBase64String(stored.Substring(Prefix.Length));
            if (payload.Length < NonceSize + TagSize)
                throw new CryptographicException("PII ciphertext payload is too short to be valid.");

            var nonce = payload.AsSpan(0, NonceSize);
            var cipherBytes = payload.AsSpan(NonceSize, payload.Length - NonceSize - TagSize);
            var tag = payload.AsSpan(payload.Length - TagSize, TagSize);

            var key = ResolveKey(useAmbientKey, fixedKey);
            if (key == null)
                throw new InvalidOperationException(
                    "Cannot decrypt PII column value: no encryption key is configured (Encryption:PiiKey).");

            var plainBytes = new byte[cipherBytes.Length];
            using (var aes = new AesGcm(key, TagSize))
            {
                aes.Decrypt(nonce, cipherBytes, tag, plainBytes);
            }

            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}
