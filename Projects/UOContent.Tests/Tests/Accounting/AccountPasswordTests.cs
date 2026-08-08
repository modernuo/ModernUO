using System;
using Server.Accounting;
using Server.Accounting.Security;
using Xunit;

namespace Server.Tests.Accounting;

[Collection("Sequential UOContent Tests")]
public class AccountPasswordTests : IDisposable
{
    private const string Password = "hunter2";

    // CurrentAlgorithm is process-wide state shared with the rest of the collection.
    private readonly PasswordProtectionAlgorithm _originalAlgorithm = AccountSecurity.CurrentAlgorithm;

    public void Dispose() => AccountSecurity.CurrentAlgorithm = _originalAlgorithm;

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

    // SetPassword assigns PasswordAlgorithm before deriving the phrase from it. Reversed, the hash
    // is salted by the outgoing algorithm's rule but stored under the incoming one, which verifies
    // once and then never again.
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

        Assert.True(account.CheckPassword(Password));
        Assert.Equal(PasswordProtectionAlgorithm.Argon2, account.PasswordAlgorithm);

        // Must verify against what the rehash wrote.
        Assert.True(account.CheckPassword(Password));
        Assert.False(account.CheckPassword("wrong-password"));
    }

    [Fact]
    public void StaleArgon2Parameters_AreRehashedOnLogin()
    {
        AccountSecurity.CurrentAlgorithm = PasswordProtectionAlgorithm.Argon2;
        var account = new Account("stale-params-user", Password);

        // The shipping default before this change: Argon2i, m=8192, t=3, p=1.
        account.Password =
            "$argon2i$v=19$m=8192,t=3,p=1$LD1XJz7P3wQmIJ+Tu6ScgA$NO5hBABsHQ172C5nDO2X4gWnB4jDef3x6WhLdVE2LFw";

        Assert.True(account.CheckPassword(Password));
        Assert.StartsWith("$argon2id$v=19$m=16384,t=1,p=1$", account.Password);

        // Already current: verifying again must not rewrite the hash.
        var afterFirst = account.Password;
        Assert.True(account.CheckPassword(Password));
        Assert.Equal(afterFirst, account.Password);
    }
}
