using System;
using Server.Accounting;
using Server.Accounting.Security;
using Xunit;

namespace Server.Tests.Accounting;

[Collection("Sequential UOContent Tests")]
public class PasswordWorkerTests : IDisposable
{
    private const string Password = "hunter2";

    private readonly PasswordProtectionAlgorithm _originalAlgorithm = AccountSecurity.CurrentAlgorithm;

    public PasswordWorkerTests() => AccountSecurity.CurrentAlgorithm = PasswordProtectionAlgorithm.Argon2;

    public void Dispose() => AccountSecurity.CurrentAlgorithm = _originalAlgorithm;

    private static Account CreateAccount(string username) =>
        Accounts.GetAccount(username) as Account ?? new Account(username, Password);

    private static PasswordJob JobFor(Account account, string submitted) =>
        new()
        {
            Account = account,
            StoredHash = account.Password,
            VerifyPhrase = account.GetVerifyPhrase(submitted),
            HashPhrase = account.NeedsPasswordUpgrade() ? account.GetRehashPhrase(submitted) : null,
            TargetAlgorithm = AccountSecurity.CurrentAlgorithm
        };

    /// <summary>
    /// Drives the real queue rather than <c>ComputeInline</c>. A job with no NetState attached -- an
    /// admin password change -- was being dropped by the liveness check, which read a null State as
    /// a dead connection, so the change silently never happened and its callback never fired.
    /// </summary>
    [Fact]
    public void RunsAJobThatHasNoConnectionAttached()
    {
        var account = CreateAccount("offloop-no-netstate-user");
        var applied = false;

        var job = new PasswordJob
        {
            Account = account,
            HashPhrase = account.GetRehashPhrase("a-queued-password"),
            TargetAlgorithm = AccountSecurity.CurrentAlgorithm,
            Sequence = account.BeginPasswordWrite(),
            OnComplete = (_, outcome) => applied = outcome.Hash != null
        };

        Assert.True(PasswordWorker.TryEnqueue(job));

        // The worker posts its result to the loop context, which no loop is pumping here.
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!applied && DateTime.UtcNow < deadline)
        {
            Core.LoopContext.ExecuteTasks();
            System.Threading.Thread.Sleep(5);
        }

        Assert.True(applied);
        Assert.True(account.CheckPassword("a-queued-password"));
    }

    [Fact]
    public void VerifiesTheCorrectPassword()
    {
        var account = CreateAccount("offloop-correct-user");

        var outcome = PasswordWorker.ComputeInline(JobFor(account, Password));

        Assert.True(outcome.Verified);
    }

    [Fact]
    public void RejectsTheWrongPassword()
    {
        var account = CreateAccount("offloop-wrong-user");

        var outcome = PasswordWorker.ComputeInline(JobFor(account, "not-the-password"));

        Assert.False(outcome.Verified);
        Assert.Null(outcome.Hash);
    }

    [Fact]
    public void ProducesNoUpgradeWhenParametersAreCurrent()
    {
        var account = CreateAccount("offloop-current-user");

        var outcome = PasswordWorker.ComputeInline(JobFor(account, Password));

        Assert.True(outcome.Verified);
        Assert.Null(outcome.Hash);
    }

    [Fact]
    public void ProducesAnUpgradeWhenParametersAreStale()
    {
        var account = CreateAccount("offloop-stale-user");

        // The shipping default before #2562: Argon2i, m=8192, t=3, p=1.
        account.Password =
            "$argon2i$v=19$m=8192,t=3,p=1$LD1XJz7P3wQmIJ+Tu6ScgA$NO5hBABsHQ172C5nDO2X4gWnB4jDef3x6WhLdVE2LFw";

        var outcome = PasswordWorker.ComputeInline(JobFor(account, Password));

        Assert.True(outcome.Verified);
        Assert.StartsWith("$argon2id$v=19$m=16384,t=1,p=1$", outcome.Hash);
    }

    [Fact]
    public void ProducesNoUpgradeWhenThePasswordIsWrong()
    {
        var account = CreateAccount("offloop-wrong-stale-user");
        account.Password =
            "$argon2i$v=19$m=8192,t=3,p=1$LD1XJz7P3wQmIJ+Tu6ScgA$NO5hBABsHQ172C5nDO2X4gWnB4jDef3x6WhLdVE2LFw";

        var outcome = PasswordWorker.ComputeInline(JobFor(account, "not-the-password"));

        Assert.False(outcome.Verified);
        Assert.Null(outcome.Hash);
    }

    [Fact]
    public void AppliesAWriteWhenNothingNewerWasRequested()
    {
        var account = CreateAccount("offloop-apply-user");

        var sequence = account.BeginPasswordWrite();
        var upgraded = Argon2PasswordProtection.Instance.EncryptPassword(Password);

        Assert.True(account.ApplyPasswordWrite(sequence, upgraded, PasswordProtectionAlgorithm.Argon2));
        Assert.Equal(upgraded, account.Password);
        Assert.True(account.CheckPassword(Password));
    }

    /// <summary>
    /// A rehash derived off-loop must not land on a password set while it ran, or the account is
    /// locked to a hash of the credential that one superseded.
    /// </summary>
    [Fact]
    public void DropsAWriteSupersededByAnInlineSetPassword()
    {
        var account = CreateAccount("offloop-stale-apply-user");

        var sequence = account.BeginPasswordWrite();
        var upgraded = Argon2PasswordProtection.Instance.EncryptPassword(Password);

        account.SetPassword("a-brand-new-password");
        var afterChange = account.Password;

        Assert.False(account.ApplyPasswordWrite(sequence, upgraded, PasswordProtectionAlgorithm.Argon2));
        Assert.Equal(afterChange, account.Password);
        Assert.True(account.CheckPassword("a-brand-new-password"));
        Assert.False(account.CheckPassword(Password));
    }

    /// <summary>
    /// Two changes dispatched before either lands: the newer must win regardless of the order the
    /// results come back in. Comparing stored hashes instead of sequences would drop the second and
    /// silently keep the older password.
    /// </summary>
    [Fact]
    public void TheNewestWriteWinsWhateverOrderResultsLand()
    {
        var account = CreateAccount("offloop-two-writes-user");

        var first = account.BeginPasswordWrite();
        var firstHash = Argon2PasswordProtection.Instance.EncryptPassword("first-new-password");

        var second = account.BeginPasswordWrite();
        var secondHash = Argon2PasswordProtection.Instance.EncryptPassword("second-new-password");

        // Results land out of order.
        Assert.True(account.ApplyPasswordWrite(second, secondHash, PasswordProtectionAlgorithm.Argon2));
        Assert.False(account.ApplyPasswordWrite(first, firstHash, PasswordProtectionAlgorithm.Argon2));

        Assert.True(account.CheckPassword("second-new-password"));
        Assert.False(account.CheckPassword("first-new-password"));
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
