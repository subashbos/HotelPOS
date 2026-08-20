using HotelPOS.Domain.Common;
using Xunit;

namespace HotelPOS.Tests;

public class PasswordPolicyTests
{
    [Fact]
    public void MeetsComplexityRequirements_Null_ReturnsFalse()
    {
        Assert.False(PasswordPolicy.MeetsComplexityRequirements(null));
    }

    [Fact]
    public void MeetsComplexityRequirements_EmptyString_ReturnsFalse()
    {
        Assert.False(PasswordPolicy.MeetsComplexityRequirements(string.Empty));
    }

    [Fact]
    public void MeetsComplexityRequirements_MissingUppercase_ReturnsFalse()
    {
        Assert.False(PasswordPolicy.MeetsComplexityRequirements("lowercase1!"));
    }

    [Fact]
    public void MeetsComplexityRequirements_MissingLowercase_ReturnsFalse()
    {
        Assert.False(PasswordPolicy.MeetsComplexityRequirements("UPPERCASE1!"));
    }

    [Fact]
    public void MeetsComplexityRequirements_MissingDigit_ReturnsFalse()
    {
        Assert.False(PasswordPolicy.MeetsComplexityRequirements("NoDigitsHere!"));
    }

    [Fact]
    public void MeetsComplexityRequirements_MissingSpecialChar_ReturnsFalse()
    {
        Assert.False(PasswordPolicy.MeetsComplexityRequirements("NoSpecial1Here"));
    }

    [Fact]
    public void MeetsComplexityRequirements_ValidPassword_ReturnsTrue()
    {
        Assert.True(PasswordPolicy.MeetsComplexityRequirements("Valid1Password!"));
    }

    [Fact]
    public void MeetsComplexityRequirements_ShortButComplex_ReturnsFalse()
    {
        Assert.False(PasswordPolicy.MeetsComplexityRequirements("Aa1!"));
    }

    [Fact]
    public void MeetsComplexityRequirements_ExactlyTenChars_ReturnsTrue()
    {
        Assert.True(PasswordPolicy.MeetsComplexityRequirements("Aa1!Aa1!Aa"));
    }

    [Fact]
    public void MeetsComplexityRequirements_NineChars_ReturnsFalse()
    {
        Assert.False(PasswordPolicy.MeetsComplexityRequirements("Aa1!Aa1!A"));
    }
}
