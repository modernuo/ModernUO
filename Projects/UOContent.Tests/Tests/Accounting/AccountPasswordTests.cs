using System;
using Server.Accounting;
using Server.Accounting.Security;
using Xunit;

namespace Server.Tests.Accounting;

[Collection("Sequential UOContent Tests")]
public class AccountPasswordTests : IDisposable
{
    private const string Password = "hunter2";

    // AccountSecurity.CurrentAlgorithm is process-wide static state, shared with every other
    // class in the "Sequential UOContent Tests" collection. xUnit constructs/disposes this class
    // once per test case, so capturing and restoring it here means every case -- current and any
    // added later to this file -- starts from and leaves behind the ambient value, instead of
    // bleeding whatever algorithm it last set into the rest of the collection.
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

    [Fact]
    public void StaleArgon2Parameters_AreRehashedOnLogin()
    {
        AccountSecurity.CurrentAlgorithm = PasswordProtectionAlgorithm.Argon2;
        var account = new Account("stale-params-user", Password);

        // Simulate an account stored under the pre-1.20.0 default: Argon2i, m=8192, t=3, p=1.
        account.Password =
            "$argon2i$v=19$m=8192,t=3,p=1$LD1XJz7P3wQmIJ+Tu6ScgA$NO5hBABsHQ172C5nDO2X4gWnB4jDef3x6WhLdVE2LFw";

        Assert.True(account.CheckPassword("hunter2"));
        Assert.StartsWith("$argon2id$v=19$m=16384,t=1,p=1$", account.Password);

        // Already current: verifying again must not rewrite the hash.
        var afterFirst = account.Password;
        Assert.True(account.CheckPassword("hunter2"));
        Assert.Equal(afterFirst, account.Password);
    }
}
