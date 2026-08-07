using Server.Accounting;
using Server.Accounting.Security;
using Xunit;

namespace Server.Tests.Accounting;

[Collection("Sequential UOContent Tests")]
public class AccountPasswordTests
{
    private const string Password = "hunter2";

    [Theory]
    [InlineData(PasswordProtectionAlgorithm.SHA1)]
    [InlineData(PasswordProtectionAlgorithm.SHA2)]
    [InlineData(PasswordProtectionAlgorithm.PBKDF2)]
    [InlineData(PasswordProtectionAlgorithm.Argon2)]
    public void NewAccount_CanLogIn(PasswordProtectionAlgorithm algorithm)
    {
        AccountSecurity.CurrentAlgorithm = algorithm;
        var account = new Account($"new-{algorithm}-user", Password);

        Assert.Equal(algorithm, account.PasswordAlgorithm);
        Assert.True(account.CheckPassword(Password));
        Assert.False(account.CheckPassword("wrong-password"));
    }

    [Theory]
    [InlineData(PasswordProtectionAlgorithm.SHA1)]
    [InlineData(PasswordProtectionAlgorithm.SHA2)]
    [InlineData(PasswordProtectionAlgorithm.PBKDF2)]
    public void UpgradingAlgorithm_DoesNotLockTheAccountOut(PasswordProtectionAlgorithm from)
    {
        AccountSecurity.CurrentAlgorithm = from;
        var account = new Account($"upgrade-{from}-user", Password);
        Assert.True(account.CheckPassword(Password));

        AccountSecurity.CurrentAlgorithm = PasswordProtectionAlgorithm.Argon2;

        // First login verifies under the old algorithm and rehashes under the new one.
        Assert.True(account.CheckPassword(Password));
        Assert.Equal(PasswordProtectionAlgorithm.Argon2, account.PasswordAlgorithm);

        // Second login must verify against what the first one wrote.
        Assert.True(account.CheckPassword(Password));
        Assert.False(account.CheckPassword("wrong-password"));
    }
}
