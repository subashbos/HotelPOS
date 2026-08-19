using System;
using System.Security.Cryptography;
using HotelPOS.Domain.Common;
using HotelPOS.Domain.Common.Constants;
using Xunit;

namespace HotelPOS.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_ThenVerify_CorrectPassword_Succeeds()
    {
        var (hash, salt) = PasswordHasher.Hash("Sup3rSecret!x");

        Assert.True(PasswordHasher.Verify("Sup3rSecret!x", hash, salt));
    }

    [Fact]
    public void Verify_WrongPassword_Fails()
    {
        var (hash, salt) = PasswordHasher.Hash("Sup3rSecret!x");

        Assert.False(PasswordHasher.Verify("WrongPassword!", hash, salt));
    }

    [Fact]
    public void Hash_EncodesCurrentIterationCountIntoStoredHash()
    {
        var (hash, _) = PasswordHasher.Hash("Sup3rSecret!x");

        Assert.StartsWith(ValidationLimits.Pbkdf2Iterations + ":", hash);
    }

    [Fact]
    public void Hash_ProducesDifferentSaltAndHashEachTime()
    {
        var (hash1, salt1) = PasswordHasher.Hash("Sup3rSecret!x");
        var (hash2, salt2) = PasswordHasher.Hash("Sup3rSecret!x");

        Assert.NotEqual(salt1, salt2);
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void NeedsRehash_FreshHash_ReturnsFalse()
    {
        var (hash, _) = PasswordHasher.Hash("Sup3rSecret!x");

        Assert.False(PasswordHasher.NeedsRehash(hash));
    }

    // ── Backward compatibility with hashes written before the iteration count was embedded ──

    [Fact]
    public void Verify_LegacyBareBase64Hash_StillVerifiesAtLegacyIterationCount()
    {
        // Simulates a row written by the old code, which stored a plain base64 PBKDF2 hash
        // (no "iterations:" prefix) computed at the fixed legacy iteration count.
        var saltBytes = RandomNumberGenerator.GetBytes(ValidationLimits.SaltByteSize);
        var legacyHashBytes = Rfc2898DeriveBytes.Pbkdf2(
            "OldPassword1!", saltBytes, ValidationLimits.Pbkdf2LegacyIterations, HashAlgorithmName.SHA256, ValidationLimits.HashByteSize);

        var legacyHash = Convert.ToBase64String(legacyHashBytes);
        var salt = Convert.ToBase64String(saltBytes);

        Assert.True(PasswordHasher.Verify("OldPassword1!", legacyHash, salt));
        Assert.False(PasswordHasher.Verify("WrongPassword!", legacyHash, salt));
    }

    [Fact]
    public void NeedsRehash_LegacyBareBase64Hash_ReturnsTrue()
    {
        var saltBytes = RandomNumberGenerator.GetBytes(ValidationLimits.SaltByteSize);
        var legacyHashBytes = Rfc2898DeriveBytes.Pbkdf2(
            "OldPassword1!", saltBytes, ValidationLimits.Pbkdf2LegacyIterations, HashAlgorithmName.SHA256, ValidationLimits.HashByteSize);
        var legacyHash = Convert.ToBase64String(legacyHashBytes);

        Assert.True(PasswordHasher.NeedsRehash(legacyHash));
    }
}
