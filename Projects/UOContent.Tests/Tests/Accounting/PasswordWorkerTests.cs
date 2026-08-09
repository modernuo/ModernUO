using System;
using System.Threading;
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

    /// <summary>
    /// Enqueues work, then pumps the loop context until <paramref name="complete"/> or the deadline.
    ///
    /// The context pins itself to the thread that constructed it and refuses <c>ExecuteTasks</c>
    /// from any other. The fixture's belongs to whichever thread built the fixture, and xUnit gives
    /// no guarantee that a test method runs on that thread even inside a sequential collection --
    /// so this owns one for the duration and puts the original back. Pumping the fixture's context
    /// passed locally and failed on CI.
    /// </summary>
    private static void PumpUntil(Action enqueue, Func<bool> complete, int timeoutSeconds = 20)
    {
        var original = Core.LoopContext;
        var owned = new EventLoopContext();
        Core.LoopContext = owned;

        try
        {
            enqueue();

            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

            while (!complete() && DateTime.UtcNow < deadline)
            {
                owned.ExecuteTasks();
                Thread.Sleep(5);
            }

            // Anything that landed between the last pump and the final check.
            owned.ExecuteTasks();
        }
        finally
        {
            Core.LoopContext = original;
        }
    }

    private static PasswordJob JobFor(Account account, string submitted) =>
        new()
        {
            Account = account,
            StoredHash = account.Password,
            VerifyPhrase = account.GetVerifyPhrase(submitted),
            HashPhrase = account.NeedsPasswordUpgrade() ? account.GetRehashPhrase(submitted) : null,
            StoredAlgorithm = account.PasswordAlgorithm,
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
            OnComplete = (_, outcome) => applied = outcome.Hash != null
        };

        PumpUntil(() => Assert.True(PasswordWorker.TryEnqueue(job)), () => applied);

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
    public void AppliesAWrite()
    {
        var account = CreateAccount("offloop-apply-user");
        var upgraded = Argon2PasswordProtection.Instance.EncryptPassword(Password);

        account.ApplyPasswordWrite(upgraded, PasswordProtectionAlgorithm.Argon2);

        Assert.Equal(upgraded, account.Password);
        Assert.True(account.CheckPassword(Password));
    }

    /// <summary>
    /// Writes apply in dispatch order, which is what makes a guard unnecessary: dispatch is on the
    /// loop, one worker drains FIFO, and results return through the loop context in that same order.
    /// A second worker thread would break this and would need ordering reintroduced.
    /// </summary>
    [Fact]
    public void WritesApplyInDispatchOrder()
    {
        var account = CreateAccount("offloop-two-writes-user");
        var done = 0;

        PumpUntil(
            () =>
            {
                for (var i = 1; i <= 2; i++)
                {
                    Assert.True(
                        PasswordWorker.TryEnqueue(
                            new PasswordJob
                            {
                                Account = account,
                                HashPhrase = account.GetRehashPhrase($"password-{i}"),
                                StoredAlgorithm = account.PasswordAlgorithm,
                                TargetAlgorithm = AccountSecurity.CurrentAlgorithm,
                                OnComplete = (_, _) => done++
                            }
                        )
                    );
                }
            },
            () => done >= 2
        );

        Assert.Equal(2, done);
        Assert.True(account.CheckPassword("password-2"));
        Assert.False(account.CheckPassword("password-1"));
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
