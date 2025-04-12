using Server.Misc;
using Xunit;

namespace Server.Tests.Accounting;

public class AccountHandlerTests
{
    [Theory]
    [InlineData("", false)]                 // Empty username
    [InlineData(" ", false)]                // Single space
    [InlineData("Invalid<Char", false)]     // Contains forbidden character
    [InlineData("EndsWithSpace ", false)]   // Ends with space
    [InlineData("EndsWithPeriod.", false)]  // Ends with period
    [InlineData(" StartsWithSpace", false)] // Starts with space
    [InlineData("ValidUser123", true)]      // Standard Username
    [InlineData("Valid.User", true)]        // Valid with period
    [InlineData("ValidUser!@#", true)]      // Contains valid special characters
    public void IsValidUsername_ValidatesCorrectly(string username, bool expected)
    {
        var result = AccountHandler.IsValidUsername(username);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("", false)]                             // Empty password
    [InlineData("Invalid\x01Char", false)]              // Contains invalid ASCII character
    [InlineData("ValidPass123!", true)]                 // Standard Password
    [InlineData(" ", true)]                             // Single space
    [InlineData("ValidPass!@#", true)]                  // Valid special characters
    [InlineData("ValidPassWithLength1234567890", true)] // Long valid password
    public void IsValidPassword_ValidatesCorrectly(string password, bool expected)
    {
        var result = AccountHandler.IsValidPassword(password);
        Assert.Equal(expected, result);
    }
}
