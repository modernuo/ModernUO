using System;
using System.Net;
using Server.Accounting;
using Server.Accounting.Security;
using Server.Network;
using Server.Tests.Network;
using Xunit;

namespace Server.Tests.Network.Packets;

[Collection("Sequential UOContent Tests")]
public class AuthIdTests : IDisposable
{
    private static readonly IPAddress AddressX = IPAddress.Parse("203.0.113.10");
    private static readonly IPAddress AddressY = IPAddress.Parse("203.0.113.11");

    private readonly PasswordProtectionAlgorithm _originalAlgorithm = AccountSecurity.CurrentAlgorithm;

    public AuthIdTests()
    {
        AccountSecurity.CurrentAlgorithm = PasswordProtectionAlgorithm.Argon2;
        IncomingAccountPackets.ClearAuthIdWindow();
    }

    public void Dispose()
    {
        IncomingAccountPackets.ClearAuthIdWindow();
        AccountSecurity.CurrentAlgorithm = _originalAlgorithm;
    }

    private static IAccount CreateAccount(string username) =>
        Accounts.GetAccount(username) ?? new Account(username, "hunter2");

    private static int Register(IAccount account, IPAddress address) =>
        IncomingAccountPackets.RegisterAuthId(account, address, new ClientVersion(7, 0, 0, 0));

    [Fact]
    public void VouchesForTheAccountAndAddressItWasIssuedTo()
    {
        var account = CreateAccount("authid-match-user");
        var authId = Register(account, AddressX);

        var result = IncomingAccountPackets.ConsumeAuthId(authId, account.Username, AddressX, out var entry);

        Assert.Equal(IncomingAccountPackets.AuthIdResult.Vouched, result);
        Assert.Same(account, entry.Account);
    }

    [Fact]
    public void DoesNotVouchForADifferentAccount()
    {
        var issued = CreateAccount("authid-owner-user");
        var other = CreateAccount("authid-other-user");
        var authId = Register(issued, AddressX);

        // AccountMismatch, not Rejected: the caller must still be able to accept this login by
        // verifying the password, exactly as it did before pre-authentication existed.
        Assert.Equal(
            IncomingAccountPackets.AuthIdResult.AccountMismatch,
            IncomingAccountPackets.ConsumeAuthId(authId, other.Username, AddressX, out _)
        );
    }

    [Fact]
    public void RejectsADifferentAddress()
    {
        var account = CreateAccount("authid-switch-user");
        var authId = Register(account, AddressX);

        Assert.Equal(
            IncomingAccountPackets.AuthIdResult.Rejected,
            IncomingAccountPackets.ConsumeAuthId(authId, account.Username, AddressY, out _)
        );
    }

    [Fact]
    public void MatchesTheUsernameCaseInsensitively()
    {
        var account = CreateAccount("AuthId-Case-User");
        var authId = Register(account, AddressX);

        Assert.Equal(
            IncomingAccountPackets.AuthIdResult.Vouched,
            IncomingAccountPackets.ConsumeAuthId(authId, "authid-case-user", AddressX, out _)
        );
    }

    [Fact]
    public void MatchesAnIPv4MappedIPv6Address()
    {
        var account = CreateAccount("authid-mapped-user");
        var authId = Register(account, AddressX);

        Assert.Equal(
            IncomingAccountPackets.AuthIdResult.Vouched,
            IncomingAccountPackets.ConsumeAuthId(authId, account.Username, AddressX.MapToIPv6(), out _)
        );
    }

    [Fact]
    public void RejectsAnUnknownAuthId()
    {
        var account = CreateAccount("authid-unknown-user");
        var authId = Register(account, AddressX);

        Assert.Equal(
            IncomingAccountPackets.AuthIdResult.Rejected,
            IncomingAccountPackets.ConsumeAuthId(authId + 1, account.Username, AddressX, out _)
        );
    }

    [Fact]
    public void IsSingleUseAfterASuccess()
    {
        var account = CreateAccount("authid-once-user");
        var authId = Register(account, AddressX);

        Assert.Equal(
            IncomingAccountPackets.AuthIdResult.Vouched,
            IncomingAccountPackets.ConsumeAuthId(authId, account.Username, AddressX, out _)
        );
        Assert.Equal(
            IncomingAccountPackets.AuthIdResult.Rejected,
            IncomingAccountPackets.ConsumeAuthId(authId, account.Username, AddressX, out _)
        );
    }

    [Fact]
    public void IsSpentEvenWhenTheAccountDoesNotMatch()
    {
        var account = CreateAccount("authid-spent-user");
        var authId = Register(account, AddressX);

        Assert.Equal(
            IncomingAccountPackets.AuthIdResult.AccountMismatch,
            IncomingAccountPackets.ConsumeAuthId(authId, "not-the-owner", AddressX, out _)
        );

        // Spent regardless, so a guessed id cannot be reused to enumerate usernames.
        Assert.Equal(
            IncomingAccountPackets.AuthIdResult.Rejected,
            IncomingAccountPackets.ConsumeAuthId(authId, account.Username, AddressX, out _)
        );
    }

    [Fact]
    public void PreAuthenticatedGameLogin_SkipsThePasswordCheck()
    {
        var account = CreateAccount("authid-preauth-user");
        using var ns = PacketTestUtilities.CreateTestNetState();

        // A wrong password is accepted only because the auth id already vouched for the account.
        var e = new GameServer.GameLoginEventArgs(ns, account.Username, "wrong-password", true);
        GameServer.GameServerLoginEvent(e);

        Assert.True(e.Accepted);
    }

    [Fact]
    public void GameLoginWithoutPreAuthentication_StillChecksThePassword()
    {
        var account = CreateAccount("authid-nopreauth-user");
        using var ns = PacketTestUtilities.CreateTestNetState();

        var wrong = new GameServer.GameLoginEventArgs(ns, account.Username, "wrong-password", false);
        GameServer.GameServerLoginEvent(wrong);
        Assert.False(wrong.Accepted);

        var right = new GameServer.GameLoginEventArgs(ns, account.Username, "hunter2", false);
        GameServer.GameServerLoginEvent(right);
        Assert.True(right.Accepted);
    }

    [Fact]
    public void GeneratesDistinctAuthIds()
    {
        var account = CreateAccount("authid-distinct-user");

        Assert.NotEqual(Register(account, AddressX), Register(account, AddressX));
    }
}
