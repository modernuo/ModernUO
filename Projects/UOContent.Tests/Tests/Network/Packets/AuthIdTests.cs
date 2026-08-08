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
    public void RejectsADifferentAccount()
    {
        var issued = CreateAccount("authid-owner-user");
        var other = CreateAccount("authid-other-user");
        var authId = Register(issued, AddressX);

        Assert.Equal(
            IncomingAccountPackets.AuthIdResult.Rejected,
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

    // A rejected attempt must not consume the id, or anyone landing on a live one could burn it and
    // force its owner to log in again.
    [Fact]
    public void SurvivesAnAttemptFromTheWrongAddress()
    {
        var account = CreateAccount("authid-not-burned-address-user");
        var authId = Register(account, AddressX);

        Assert.Equal(
            IncomingAccountPackets.AuthIdResult.Rejected,
            IncomingAccountPackets.ConsumeAuthId(authId, account.Username, AddressY, out _)
        );

        Assert.Equal(1, IncomingAccountPackets.AuthIdWindowCount);
        Assert.Equal(
            IncomingAccountPackets.AuthIdResult.Vouched,
            IncomingAccountPackets.ConsumeAuthId(authId, account.Username, AddressX, out _)
        );
    }

    [Fact]
    public void SurvivesAnAttemptForTheWrongAccount()
    {
        var account = CreateAccount("authid-not-burned-account-user");
        var authId = Register(account, AddressX);

        Assert.Equal(
            IncomingAccountPackets.AuthIdResult.Rejected,
            IncomingAccountPackets.ConsumeAuthId(authId, "not-the-owner", AddressX, out _)
        );

        Assert.Equal(1, IncomingAccountPackets.AuthIdWindowCount);
        Assert.Equal(
            IncomingAccountPackets.AuthIdResult.Vouched,
            IncomingAccountPackets.ConsumeAuthId(authId, account.Username, AddressX, out _)
        );
    }

    [Fact]
    public void ARejectedAttemptYieldsNoEntry()
    {
        var account = CreateAccount("authid-no-leak-user");
        var authId = Register(account, AddressX);

        IncomingAccountPackets.ConsumeAuthId(authId, "not-the-owner", AddressX, out var entry);

        Assert.Null(entry.Account);
    }

    [Fact]
    public void AnExpiredIdIsSpentByItsOwner()
    {
        var account = CreateAccount("authid-expired-spent-user");
        var authId = Register(account, AddressX);

        var now = Core._now;

        try
        {
            Core._now = now + TimeSpan.FromMinutes(30.0);

            Assert.Equal(
                IncomingAccountPackets.AuthIdResult.Expired,
                IncomingAccountPackets.ConsumeAuthId(authId, account.Username, AddressX, out _)
            );
            Assert.Equal(0, IncomingAccountPackets.AuthIdWindowCount);
        }
        finally
        {
            Core._now = now;
        }
    }

    // Expiry is not a lockout. The game login always verified the password before any of this
    // existed, so falling back to that verify is the behaviour we started from.
    [Fact]
    public void ExpiresIntoAPasswordVerifyRatherThanARejection()
    {
        var account = CreateAccount("authid-expired-user");
        var authId = Register(account, AddressX);

        var now = Core._now;

        try
        {
            Core._now = now + TimeSpan.FromMinutes(30.0);

            Assert.Equal(
                IncomingAccountPackets.AuthIdResult.Expired,
                IncomingAccountPackets.ConsumeAuthId(authId, account.Username, AddressX, out var entry)
            );

            // Still carries the client version the game login needs.
            Assert.Equal(new ClientVersion(7, 0, 0, 0), entry.Version);
        }
        finally
        {
            Core._now = now;
        }
    }

    [Fact]
    public void AnExpiredIdFromAnotherAddressIsStillRejected()
    {
        var account = CreateAccount("authid-expired-elsewhere-user");
        var authId = Register(account, AddressX);

        var now = Core._now;

        try
        {
            Core._now = now + TimeSpan.FromMinutes(30.0);

            Assert.Equal(
                IncomingAccountPackets.AuthIdResult.Rejected,
                IncomingAccountPackets.ConsumeAuthId(authId, account.Username, AddressY, out _)
            );
        }
        finally
        {
            Core._now = now;
        }
    }

    private static int Ensure(int existingAuthId, IAccount account, IPAddress address) =>
        IncomingAccountPackets.EnsureAuthId(
            existingAuthId,
            account,
            address,
            new ClientVersion(7, 0, 0, 0)
        );

    [Fact]
    public void IssuesAnIdWhenTheConnectionHasNone()
    {
        var account = CreateAccount("authid-first-select-user");

        var authId = Ensure(0, account, AddressX);

        Assert.NotEqual(0, authId);
        Assert.Equal(1, IncomingAccountPackets.AuthIdWindowCount);
    }

    // Handing the same id back rather than minting another is what makes an orphan impossible,
    // instead of something to clean up afterwards.
    [Fact]
    public void ReSelectingReturnsTheSameIdAndAddsNothingToTheWindow()
    {
        var account = CreateAccount("authid-reselect-user");
        var first = Ensure(0, account, AddressX);

        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(first, Ensure(first, account, AddressX));
        }

        Assert.Equal(1, IncomingAccountPackets.AuthIdWindowCount);
        Assert.Equal(
            IncomingAccountPackets.AuthIdResult.Vouched,
            IncomingAccountPackets.ConsumeAuthId(first, account.Username, AddressX, out _)
        );
    }

    [Fact]
    public void AbandonedIdsAreSweptWhenNewOnesAreIssued()
    {
        var abandoned = CreateAccount("authid-abandoned-user");
        var live = CreateAccount("authid-live-user");

        var now = Core._now;

        try
        {
            for (var i = 0; i < 128; i++)
            {
                Register(abandoned, AddressX);
            }

            Assert.Equal(128, IncomingAccountPackets.AuthIdWindowCount);

            Core._now = now + TimeSpan.FromMinutes(30.0);

            var liveId = Register(live, AddressX);

            Assert.Equal(1, IncomingAccountPackets.AuthIdWindowCount);
            Assert.Equal(
                IncomingAccountPackets.AuthIdResult.Vouched,
                IncomingAccountPackets.ConsumeAuthId(liveId, live.Username, AddressX, out _)
            );
        }
        finally
        {
            Core._now = now;
        }
    }

    // A login rush is not a backlog. Every id belongs to a client on its way to redeem it, so none
    // may be discarded to hold the window at some arbitrary size.
    [Fact]
    public void ALoginRushDoesNotEvictAnyonesAuthId()
    {
        var account = CreateAccount("authid-rush-user");
        var ids = new int[800];

        for (var i = 0; i < ids.Length; i++)
        {
            ids[i] = Register(account, AddressX);
        }

        Assert.Equal(ids.Length, IncomingAccountPackets.AuthIdWindowCount);

        // Every id issued during the rush is still redeemable, including the first one.
        for (var i = 0; i < ids.Length; i++)
        {
            Assert.Equal(
                IncomingAccountPackets.AuthIdResult.Vouched,
                IncomingAccountPackets.ConsumeAuthId(ids[i], account.Username, AddressX, out _)
            );
        }
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
