using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using Server.Accounting;
using Server.Accounting.Security;
using Server.Misc;
using Server.Network;
using Xunit;

namespace Server.Tests.Accounting;

[Collection("Sequential UOContent Tests")]
public class AccountHandlerTests : IDisposable
{
    private static readonly FieldInfo _addressField =
        typeof(NetState).GetField("<Address>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

    private readonly int _originalMaxLogins = AccountHandler.MaxLoginsPerIP;
    private readonly PasswordProtectionAlgorithm _originalAlgorithm = AccountSecurity.CurrentAlgorithm;
    private readonly List<Account> _accounts = [];

    // Account construction hashes a password and the fixture leaves no algorithm configured.
    public AccountHandlerTests() => AccountSecurity.CurrentAlgorithm = PasswordProtectionAlgorithm.SHA2;

    public void Dispose()
    {
        AccountHandler.MaxLoginsPerIP = _originalMaxLogins;
        AccountSecurity.CurrentAlgorithm = _originalAlgorithm;

        for (var i = 0; i < _accounts.Count; i++)
        {
            _accounts[i].Delete();
        }
    }

    [Theory]
    [InlineData("", false)]                 // Empty username
    [InlineData(" ", false)]                // Single space
    [InlineData(".", false)]                // Single period
    [InlineData("Invalid<Char", false)]     // Contains forbidden character
    [InlineData("EndsWithSpace ", false)]   // Ends with space
    [InlineData("EndsWithPeriod.", false)]  // Ends with period
    [InlineData(" StartsWithSpace", false)] // Starts with space
    [InlineData("ValidUser123", true)]      // Standard Username
    [InlineData("Valid.User", true)]        // Valid with period
    [InlineData("ValidUser!@#", true)]      // Contains valid special characters
    public void IsValidUsername_ValidatesCorrectly(string username, bool expected)
    {
        Assert.Equal(expected, AccountHandler.IsValidUsername(username));
    }

    [Fact]
    public void AccountCount_GroupsSlash64()
    {
        AccountHandler.IncrementAccountCount(IPAddress.Parse("2001:db8:5:2::1"));
        AccountHandler.IncrementAccountCount(IPAddress.Parse("2001:db8:5:2::2"));

        AccountHandler.IncrementAccountCount(IPAddress.Parse("198.51.100.4"));
        AccountHandler.IncrementAccountCount(IPAddress.Parse("198.51.100.5"));

        try
        {
            Assert.Equal(2, AccountHandler.GetAccountCount(IPAddress.Parse("2001:db8:5:2::3")));
            Assert.Equal(0, AccountHandler.GetAccountCount(IPAddress.Parse("2001:db8:5:3::1")));
            Assert.Equal(1, AccountHandler.GetAccountCount(IPAddress.Parse("198.51.100.4")));
            Assert.Equal(1, AccountHandler.GetAccountCount(IPAddress.Parse("198.51.100.5")));
        }
        finally
        {
            AccountHandler.DecrementAccountCount(IPAddress.Parse("2001:db8:5:2::1"));
            AccountHandler.DecrementAccountCount(IPAddress.Parse("2001:db8:5:2::2"));
            AccountHandler.DecrementAccountCount(IPAddress.Parse("198.51.100.4"));
            AccountHandler.DecrementAccountCount(IPAddress.Parse("198.51.100.5"));
        }

        // Floors at zero rather than going negative.
        AccountHandler.DecrementAccountCount(IPAddress.Parse("2001:db8:5:2::1"));
        Assert.Equal(0, AccountHandler.GetAccountCount(IPAddress.Parse("2001:db8:5:2::1")));
    }

    [Fact]
    public void ConcurrentCap_Disabled_ByDefault()
    {
        AccountHandler.MaxLoginsPerIP = 0;

        var acct = CreateAccount("cap-disabled-new");
        var state = CreateNetState("2001:db8:6:1::10", null);
        var candidates = new HashSet<NetState>
        {
            state,
            CreateNetState("2001:db8:6:1::1", CreateAccount("cap-disabled-a")),
            CreateNetState("2001:db8:6:1::2", CreateAccount("cap-disabled-b"))
        };

        Assert.False(AccountHandler.IsAtLoginCap(state, acct, candidates));
    }

    [Fact]
    public void ConcurrentCap_BlocksPastLimit()
    {
        AccountHandler.MaxLoginsPerIP = 2;

        var acct = CreateAccount("cap-block-new");
        var candidates = new HashSet<NetState>
        {
            CreateNetState("2001:db8:7:1::1", CreateAccount("cap-block-a")),
            CreateNetState("2001:db8:7:1::2", CreateAccount("cap-block-b"))
        };

        var sameSubscriber = CreateNetState("2001:db8:7:1::99", null);
        var otherSubscriber = CreateNetState("2001:db8:7:2::99", null);

        Assert.True(AccountHandler.IsAtLoginCap(sameSubscriber, acct, candidates));
        Assert.False(AccountHandler.IsAtLoginCap(otherSubscriber, acct, candidates));
    }

    [Fact]
    public void ConcurrentCap_CountsDistinctAccounts()
    {
        AccountHandler.MaxLoginsPerIP = 2;

        var acct = CreateAccount("cap-distinct-new");
        var other = CreateAccount("cap-distinct-a");

        // Login-server and game-server connections of one account are one login.
        var candidates = new HashSet<NetState>
        {
            CreateNetState("2001:db8:8:1::1", other),
            CreateNetState("2001:db8:8:1::1", other)
        };

        Assert.False(AccountHandler.IsAtLoginCap(CreateNetState("2001:db8:8:1::99", null), acct, candidates));
    }

    [Fact]
    public void ConcurrentCap_SameAccount_DoesNotCount()
    {
        AccountHandler.MaxLoginsPerIP = 1;

        var acct = CreateAccount("cap-same-new");
        var candidates = new HashSet<NetState> { CreateNetState("2001:db8:9:1::1", acct) };

        Assert.False(AccountHandler.IsAtLoginCap(CreateNetState("2001:db8:9:1::2", null), acct, candidates));
    }

    [Fact]
    public void ConcurrentCap_StaffExempt()
    {
        AccountHandler.MaxLoginsPerIP = 1;

        var player = CreateAccount("cap-staff-player");
        var otherPlayer = CreateAccount("cap-staff-other");
        var staff = CreateAccount("cap-staff-gm");
        staff.AccessLevel = AccessLevel.GameMaster;

        var state = CreateNetState("2001:db8:a:1::99", null);

        var withPlayer = new HashSet<NetState> { CreateNetState("2001:db8:a:1::1", otherPlayer) };
        Assert.False(AccountHandler.IsAtLoginCap(state, staff, withPlayer));

        var withStaff = new HashSet<NetState> { CreateNetState("2001:db8:a:1::1", staff) };
        Assert.False(AccountHandler.IsAtLoginCap(state, player, withStaff));
    }

    private Account CreateAccount(string username)
    {
        var acct = new Account($"{username}-{Guid.NewGuid():N}", "hunter2");
        _accounts.Add(acct);
        return acct;
    }

    // NetState's only constructor accepts a live socket and registers globally; the cap only reads
    // Address and Account, so a bare instance is enough.
    private static NetState CreateNetState(string address, Account account)
    {
        var ns = (NetState)RuntimeHelpers.GetUninitializedObject(typeof(NetState));
        _addressField.SetValue(ns, IPAddress.Parse(address));
        ns.Account = account;
        return ns;
    }
}
