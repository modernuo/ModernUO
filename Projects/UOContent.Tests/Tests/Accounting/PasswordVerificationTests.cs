using System;
using Server.Accounting;
using Server.Accounting.Security;
using Xunit;

namespace Server.Tests.Accounting;

[Collection("Sequential UOContent Tests")]
public class PasswordVerificationTests : IDisposable
{
    private const string Password = "hunter2";

    private readonly PasswordProtectionAlgorithm _originalAlgorithm = AccountSecurity.CurrentAlgorithm;

    public PasswordVerificationTests() => AccountSecurity.CurrentAlgorithm = PasswordProtectionAlgorithm.Argon2;

    public void Dispose() => AccountSecurity.CurrentAlgorithm = _originalAlgorithm;

    private static Account CreateAccount(string username) =>
        Accounts.GetAccount(username) as Account ?? new Account(username, Password);

    private static PasswordVerificationJob JobFor(Account account, string submitted) =>
        new()
        {
            Account = account,
            StoredHash = account.Password,
            VerifyPhrase = account.GetVerifyPhrase(submitted),
            RehashPhrase = account.NeedsPasswordUpgrade() ? account.GetRehashPhrase(submitted) : null,
            TargetAlgorithm = AccountSecurity.CurrentAlgorithm
        };

    [Fact]
    public void VerifiesTheCorrectPassword()
    {
        var account = CreateAccount("offloop-correct-user");

        var outcome = PasswordVerificationWorker.ComputeInline(JobFor(account, Password));

        Assert.True(outcome.Verified);
    }

    [Fact]
    public void RejectsTheWrongPassword()
    {
        var account = CreateAccount("offloop-wrong-user");

        var outcome = PasswordVerificationWorker.ComputeInline(JobFor(account, "not-the-password"));

        Assert.False(outcome.Verified);
        Assert.Null(outcome.UpgradedPassword);
    }

    [Fact]
    public void ProducesNoUpgradeWhenParametersAreCurrent()
    {
        var account = CreateAccount("offloop-current-user");

        var outcome = PasswordVerificationWorker.ComputeInline(JobFor(account, Password));

        Assert.True(outcome.Verified);
        Assert.Null(outcome.UpgradedPassword);
    }

    [Fact]
    public void ProducesAnUpgradeWhenParametersAreStale()
    {
        var account = CreateAccount("offloop-stale-user");

        // The shipping default before #2562: Argon2i, m=8192, t=3, p=1.
        account.Password =
            "$argon2i$v=19$m=8192,t=3,p=1$LD1XJz7P3wQmIJ+Tu6ScgA$NO5hBABsHQ172C5nDO2X4gWnB4jDef3x6WhLdVE2LFw";

        var outcome = PasswordVerificationWorker.ComputeInline(JobFor(account, Password));

        Assert.True(outcome.Verified);
        Assert.StartsWith("$argon2id$v=19$m=16384,t=1,p=1$", outcome.UpgradedPassword);
    }

    [Fact]
    public void ProducesNoUpgradeWhenThePasswordIsWrong()
    {
        var account = CreateAccount("offloop-wrong-stale-user");
        account.Password =
            "$argon2i$v=19$m=8192,t=3,p=1$LD1XJz7P3wQmIJ+Tu6ScgA$NO5hBABsHQ172C5nDO2X4gWnB4jDef3x6WhLdVE2LFw";

        var outcome = PasswordVerificationWorker.ComputeInline(JobFor(account, "not-the-password"));

        Assert.False(outcome.Verified);
        Assert.Null(outcome.UpgradedPassword);
    }

    [Fact]
    public void AppliesAnUpgradeWhenThePasswordIsUnchanged()
    {
        var account = CreateAccount("offloop-apply-user");
        var stored = account.Password;

        var upgraded = Argon2PasswordProtection.Instance.EncryptPassword(Password);
        account.ApplyPasswordUpgrade(stored, upgraded, PasswordProtectionAlgorithm.Argon2);

        Assert.Equal(upgraded, account.Password);
        Assert.True(account.CheckPassword(Password));
    }

    /// <summary>
    /// The verify runs off-loop for ~9 ms. A password changed in that window was already written
    /// with current parameters; applying the stale upgrade would replace it with a hash of the
    /// previous password and lock the account out.
    /// </summary>
    [Fact]
    public void DropsAnUpgradeWhenThePasswordChangedMeanwhile()
    {
        var account = CreateAccount("offloop-stale-apply-user");
        var storedAtDispatch = account.Password;

        // Derived from the old password, as the worker would have.
        var upgraded = Argon2PasswordProtection.Instance.EncryptPassword(Password);

        // ...and the password changes before the verdict lands.
        account.SetPassword("a-brand-new-password");
        var afterChange = account.Password;

        account.ApplyPasswordUpgrade(storedAtDispatch, upgraded, PasswordProtectionAlgorithm.Argon2);

        Assert.Equal(afterChange, account.Password);
        Assert.True(account.CheckPassword("a-brand-new-password"));
        Assert.False(account.CheckPassword(Password));
    }

    [Theory]
    [InlineData(PasswordProtectionAlgorithm.SHA1)]
    [InlineData(PasswordProtectionAlgorithm.SHA2)]
    public void UsesTheUsernameSaltedPhraseForShaAccounts(PasswordProtectionAlgorithm algorithm)
    {
        AccountSecurity.CurrentAlgorithm = algorithm;
        var account = CreateAccount($"offloop-phrase-{algorithm}-user");

        // Verification must use the algorithm the hash was stored under...
        Assert.Equal($"{account.Username}{Password}", account.GetVerifyPhrase(Password));

        // ...and a rehash the one it is moving to. Swapping these is the #2562 lockout.
        AccountSecurity.CurrentAlgorithm = PasswordProtectionAlgorithm.Argon2;
        Assert.Equal(Password, account.GetRehashPhrase(Password));
    }

    [Fact]
    public void UsesTheBarePasswordForArgon2Accounts()
    {
        var account = CreateAccount("offloop-phrase-argon2-user");

        Assert.Equal(Password, account.GetVerifyPhrase(Password));
        Assert.Equal(Password, account.GetRehashPhrase(Password));
    }
}
