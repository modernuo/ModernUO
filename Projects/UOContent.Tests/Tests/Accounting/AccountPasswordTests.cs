using System;
using Server.Accounting;
using Server.Accounting.Security;
using Xunit;

namespace Server.Tests.Accounting;

[Collection("Sequential UOContent Tests")]
public class AccountPasswordTests : IDisposable
{
    private const string Password = "hunter2";

    // AccountSecurity.CurrentAlgorithm and AccountSecurity.RepairMigratedPasswords are process-wide
    // static state, shared with every other class in the "Sequential UOContent Tests" collection.
    // xUnit constructs/disposes this class once per test case, so capturing and restoring them here
    // means every case -- current and any added later to this file -- starts from and leaves behind
    // the ambient values, instead of bleeding whatever it last set into the rest of the collection.
    private readonly PasswordProtectionAlgorithm _originalAlgorithm = AccountSecurity.CurrentAlgorithm;
    private readonly bool _originalRepairMigratedPasswords = AccountSecurity.RepairMigratedPasswords;

    public void Dispose()
    {
        AccountSecurity.CurrentAlgorithm = _originalAlgorithm;
        AccountSecurity.RepairMigratedPasswords = _originalRepairMigratedPasswords;
    }

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

    /// <summary>
    /// Builds an account whose stored hash is what the pre-fix SetPassword produced when a SHA2
    /// account was upgraded to Argon2: argon2(username + password), tagged Argon2, whose phrase
    /// rule omits the username. Unrecoverable without the plaintext, hence the repair path.
    /// </summary>
    private static Account CreateMisMigratedAccount(string username)
    {
        AccountSecurity.CurrentAlgorithm = PasswordProtectionAlgorithm.Argon2;
        var account = new Account(username, Password);
        account.Password = AccountSecurity.CurrentPasswordProtection
            .EncryptPassword($"{username}{Password}");

        return account;
    }

    /// <summary>
    /// The other corruption shape: an account *created* while CurrentAlgorithm was SHA1 or SHA2.
    /// The pre-fix SetPassword ran from the constructor before _passwordAlgorithm was assigned, so
    /// it took the None branch and stored H(bare password) under an algorithm whose phrase rule
    /// adds the username. Those accounts could never log in at all.
    /// </summary>
    private static Account CreateMisCreatedShaAccount(string username, PasswordProtectionAlgorithm algorithm)
    {
        AccountSecurity.CurrentAlgorithm = algorithm;
        var account = new Account(username, Password);
        account.Password = AccountSecurity.CurrentPasswordProtection.EncryptPassword(Password);

        return account;
    }

    [Fact]
    public void MisMigratedAccount_IsRejected_WhenRepairIsDisabled()
    {
        var account = CreateMisMigratedAccount("repair-off-user");
        account.SetTag(Account.RepairPasswordTag, "yes");
        AccountSecurity.RepairMigratedPasswords = false;

        Assert.False(account.CheckPassword(Password));
    }

    [Fact]
    public void MisMigratedAccount_IsRejected_WhenTheAccountIsNotTagged()
    {
        var account = CreateMisMigratedAccount("repair-untagged-user");
        AccountSecurity.RepairMigratedPasswords = true;

        Assert.Null(account.GetTag(Account.RepairPasswordTag));
        Assert.False(account.CheckPassword(Password));
    }

    [Fact]
    public void MisMigratedAccount_IsRepaired_WhenRepairIsEnabled()
    {
        var account = CreateMisMigratedAccount("repair-on-user");
        account.SetTag(Account.RepairPasswordTag, "yes");
        AccountSecurity.RepairMigratedPasswords = true;

        Assert.True(account.CheckPassword(Password));

        AccountSecurity.RepairMigratedPasswords = false;

        // Repaired in place: it must now verify with the flag back off.
        Assert.True(account.CheckPassword(Password));
        Assert.False(account.CheckPassword("wrong-password"));
    }

    [Theory]
    [InlineData(PasswordProtectionAlgorithm.SHA1)]
    [InlineData(PasswordProtectionAlgorithm.SHA2)]
    public void MisCreatedShaAccount_IsRepaired_WhenBothGatesAreSet(PasswordProtectionAlgorithm algorithm)
    {
        var account = CreateMisCreatedShaAccount($"repair-created-{algorithm}-user", algorithm);
        account.SetTag(Account.RepairPasswordTag, "yes");
        AccountSecurity.RepairMigratedPasswords = true;

        Assert.True(account.CheckPassword(Password));

        AccountSecurity.RepairMigratedPasswords = false;

        // Repaired in place under the username-salted rule its algorithm actually uses.
        Assert.True(account.CheckPassword(Password));
        Assert.False(account.CheckPassword("wrong-password"));
    }

    [Theory]
    [InlineData(PasswordProtectionAlgorithm.SHA1)]
    [InlineData(PasswordProtectionAlgorithm.SHA2)]
    public void MisCreatedShaAccount_IsRejected_WhenTheAccountIsNotTagged(PasswordProtectionAlgorithm algorithm)
    {
        var account = CreateMisCreatedShaAccount($"reject-created-{algorithm}-user", algorithm);
        AccountSecurity.RepairMigratedPasswords = true;

        Assert.False(account.CheckPassword(Password));
    }

    [Fact]
    public void RepairTag_IsClearedAfterASuccessfulRepair()
    {
        var account = CreateMisMigratedAccount("repair-one-shot-user");
        account.SetTag(Account.RepairPasswordTag, "yes");
        AccountSecurity.RepairMigratedPasswords = true;

        Assert.True(account.CheckPassword(Password));
        Assert.Null(account.GetTag(Account.RepairPasswordTag));

        // The window is closed for this account: corrupting it again is no longer repairable,
        // even with the shard-wide flag still on.
        account.Password = AccountSecurity.CurrentPasswordProtection
            .EncryptPassword($"repair-one-shot-user{Password}");

        Assert.False(account.CheckPassword(Password));
    }

    [Fact]
    public void WrongPassword_IsStillRejected_WhenRepairIsEnabled()
    {
        AccountSecurity.CurrentAlgorithm = PasswordProtectionAlgorithm.Argon2;
        var account = new Account("repair-wrong-pass-user", Password);
        account.SetTag(Account.RepairPasswordTag, "yes");
        AccountSecurity.RepairMigratedPasswords = true;

        Assert.False(account.CheckPassword("wrong-password"));
        Assert.True(account.CheckPassword(Password));
    }

    /// <summary>
    /// The repair cannot distinguish a mis-migrated hash from a password that merely begins with
    /// the username, so a shard-wide repair window would let anyone log into such an account with
    /// only the suffix -- and the rehash that follows would rewrite the stored credential down to
    /// that suffix, locking the real owner out permanently. The per-account tag is what stops it:
    /// with the flag on but the account untagged, the attack must fail and the hash must not move.
    /// </summary>
    [Fact]
    public void TruncatedPassword_IsRejected_AndDoesNotRewriteTheHash()
    {
        const string username = "trunc-attack-user";
        const string realPassword = $"{username}123";

        AccountSecurity.CurrentAlgorithm = PasswordProtectionAlgorithm.Argon2;

        // Stored correctly: argon2("trunc-attack-user123"), no corruption anywhere.
        var account = new Account(username, realPassword);
        var storedBefore = account.Password;

        AccountSecurity.RepairMigratedPasswords = true;

        Assert.False(account.CheckPassword("123"));
        Assert.Equal(storedBefore, account.Password);

        // And the owner is still able to log in afterwards.
        Assert.True(account.CheckPassword(realPassword));
    }
}
