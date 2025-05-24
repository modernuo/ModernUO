using System.Buffers;
using Server.Misc;
using Xunit;

namespace Server.Tests;

public class NameVerificationTests
{
    [Theory]
    [InlineData("John")]
    [InlineData("Mary Ann")]
    [InlineData("Bob-Jones")]
    [InlineData("O'Malley")]
    public void ValidatePlayerName_ValidNames_ReturnsTrue(string name)
    {
        Assert.True(NameVerification.ValidatePlayerName(name));
    }

    [Theory]
    [InlineData("Rex")]
    [InlineData("MrWhiskers")]
    [InlineData("Fido")]
    public void ValidatePetName_ValidNames_ReturnsTrue(string name)
    {
        Assert.True(NameVerification.ValidatePetName(name));
    }

    [Theory]
    [InlineData("Mrs-Smith")]
    [InlineData("Mr. Whiskers")]
    [InlineData("Dog123")]
    public void ValidatePetName_InvalidNames_ReturnsFalse(string name)
    {
        Assert.False(NameVerification.ValidatePetName(name));
    }

    [Theory]
    [InlineData("Blacksmith12")]
    [InlineData("Baker")]
    [InlineData("Innkeeper")]
    public void ValidateVendorName_ValidNames_ReturnsTrue(string name)
    {
        Assert.True(NameVerification.ValidateVendorName(name));
    }

    [Theory]
    [InlineData("Blacksmith 123")]
    [InlineData("Baker-Smith")]
    [InlineData("*Innkeeper*")]
    public void ValidateVendorName_ValidNames_ReturnsFalse(string name)
    {
        Assert.False(NameVerification.ValidateVendorName(name));
    }

    [Fact]
    public void Validate_MinimumLengthName_ReturnsTrue()
    {
        Assert.True(NameVerification.Validate("Ab", 2, 16, true, false));
    }

    [Fact]
    public void Validate_MaximumLengthName_ReturnsTrue()
    {
        Assert.True(NameVerification.Validate("AbcdefghijklmnopqrsT", 1, 20, true, true));
    }

    [Fact]
    public void Validate_MaxExceptions_ReturnsTrue()
    {
        var exceptions = SearchValues.Create(' ', '-', '.');
        Assert.True(NameVerification.Validate("A-B.C D", 2, 16, true, false, false, 3, exceptions));
    }

    [Fact]
    public void Validate_BoundaryOfDisallowedWord_ReturnsTrueWhenNotActuallyDisallowed()
    {
        // "ass" is in disallowed, but "class" has proper boundaries
        Assert.True(NameVerification.ValidatePlayerName("Class"));
    }

    // Negative Tests
    [Fact]
    public void Validate_EmptyName_ReturnsFalse()
    {
        Assert.False(NameVerification.Validate("", 1, 20, true, true));
    }

    [Fact]
    public void Validate_TooShortName_ReturnsFalse()
    {
        Assert.False(NameVerification.Validate("A", 2, 16, true, false));
    }

    [Fact]
    public void Validate_TooLongName_ReturnsFalse()
    {
        Assert.False(NameVerification.Validate("AbcdefghijklmnopqrstuvwxyzABCDEF", 2, 16, true, false));
    }

    [Theory]
    [InlineData("ass")]
    [InlineData("GodDamn Fine")]
    [InlineData("Fuck")]
    public void Validate_DisallowedWords_ReturnsFalse(string name)
    {
        Assert.False(NameVerification.ValidatePlayerName(name));
    }

    [Theory]
    [InlineData("GMJohn")]
    [InlineData("LordBob")]
    [InlineData("SeerMagic")]
    public void Validate_DisallowedPrefixes_ReturnsFalse(string name)
    {
        Assert.False(NameVerification.ValidatePlayerName(name));
    }

    [Fact]
    public void Validate_TooManyExceptions_ReturnsFalse()
    {
        var exceptions = SearchValues.Create(' ', '-', '.');
        Assert.False(NameVerification.Validate("A-B.C D-E", 2, 16, true, false, false, 3, exceptions));
    }

    [Fact]
    public void Validate_ExceptionAtStartWhenNotAllowed_ReturnsFalse()
    {
        var exceptions = SearchValues.Create(' ', '-', '.');
        Assert.False(NameVerification.Validate("-John", 2, 16, true, false, true, 1, exceptions));
    }

    [Fact]
    public void Validate_DisallowedCharacters_ReturnsFalse()
    {
        Assert.False(NameVerification.Validate("John123", 2, 16, true, false));
    }
}
