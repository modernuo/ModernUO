using System;
using System.Collections.Generic;
using System.Net;
using Server.Accounting;
using Server.Network;
using Xunit;

namespace Server.Tests.Network;

public class AccountPacketTests : IClassFixture<ServerFixture>
{
    private class MockedAccount : IAccount
    {
        public int TotalGold { get; }
        public int TotalPlat { get; }
        public bool DepositGold(int amount) => throw new NotImplementedException();

        public bool DepositPlat(int amount) => throw new NotImplementedException();

        public bool WithdrawGold(int amount) => throw new NotImplementedException();

        public bool WithdrawPlat(int amount) => throw new NotImplementedException();

        public long GetTotalGold() => throw new NotImplementedException();

        public int CompareTo(IAccount other) => throw new NotImplementedException();

        public string Username { get; set; }
        public string Email { get; set; }
        public AccessLevel AccessLevel { get; set; }
        public int Length { get; }
        public int Limit { get; }
        public int Count { get; }

        private Dictionary<int, Mobile> _mobiles = new();

        public Mobile this[int index]
        {
            get => _mobiles[index];
            set
            {
                _mobiles[index] = value;
                if (value != null)
                {
                    value.Account = this;
                }
            }
        }

        public void Delete()
        {
            throw new NotImplementedException();
        }

        public void SetPassword(string password)
        {
            throw new NotImplementedException();
        }

        public bool CheckPassword(string password) => throw new NotImplementedException();
    }

    [Fact]
    public void TestChangeCharacter()
    {
        var firstMobile = new Mobile((Serial)0x1);
        firstMobile.DefaultMobileInit();
        firstMobile.RawName = "Test Mobile";

        var secondMobile = new Mobile((Serial)0x2);
        secondMobile.DefaultMobileInit();
        secondMobile.RawName = null;

        var account = new MockedAccount
        {
            [0] = firstMobile,
            [1] = null,
            [2] = secondMobile
        };

        // var account = new MockAccount(new[] { firstMobile, null, secondMobile });
        var expected = new ChangeCharacter(account).Compile();

        var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendChangeCharacter(account);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestClientVersionReq()
    {
        var expected = new ClientVersionReq().Compile();

        var ns = PacketTestUtilities.CreateTestNetState();

        ns.SendClientVersionRequest();

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestDeleteResult()
    {
        var expected = new DeleteResult(DeleteResultType.BadRequest).Compile();

        var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendCharacterDeleteResult(DeleteResultType.BadRequest);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestPopupMessage()
    {
        var expected = new PopupMessage(PMMessage.LoginSyncError).Compile();

        var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendPopupMessage(PMMessage.LoginSyncError);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
    }

    [Theory, InlineData(ProtocolChanges.Version70610), InlineData(ProtocolChanges.Version6000)]
    public void TestSupportedFeatures(ProtocolChanges protocolChanges)
    {
        var firstMobile = new Mobile((Serial)0x1);
        firstMobile.DefaultMobileInit();
        firstMobile.Name = "Test Mobile";

        var account = new MockedAccount
        {
            [0] = firstMobile,
            [1] = null,
            [2] = null,
            [3] = null,
            [4] = null
        };

        var ns = PacketTestUtilities.CreateTestNetState();
        ns.Account = account;
        ns.ProtocolChanges = protocolChanges;

        var expected = new SupportedFeatures(ns).Compile();
        ns.SendSupportedFeature();

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestLoginConfirm()
    {
        var m = new Mobile((Serial)0x1);
        m.DefaultMobileInit();
        m.Body = 0x100;
        m.X = 100;
        m.Y = 10;
        m.Z = -10;
        m.Direction = Direction.Down;
        m.LogoutMap = Map.Felucca;

        var expected = new LoginConfirm(m).Compile();

        var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendLoginConfirmation(m);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestLoginComplete()
    {
        var expected = new LoginComplete().Compile();

        var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendLoginComplete();

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestCharacterListUpdate()
    {
        var firstMobile = new Mobile((Serial)0x1);
        firstMobile.DefaultMobileInit();
        firstMobile.RawName = "Test Mobile";

        var account = new MockedAccount
        {
            [0] = null,
            [1] = firstMobile,
            [2] = null,
            [3] = null,
            [4] = null
        };

        var expected = new CharacterListUpdate(account).Compile();

        var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendCharacterListUpdate(account);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestCharacterList70130()
    {
        var firstMobile = new Mobile((Serial)0x1);
        firstMobile.DefaultMobileInit();
        firstMobile.Name = "Test Mobile";

        var account = new MockedAccount
        {
            [0] = null,
            [1] = firstMobile,
            [2] = null,
            [3] = null,
            [4] = null
        };

        var info = new[]
        {
            new CityInfo("Test City", "Test Building", 50, 100, 10, -10)
        };

        var expected = new CharacterList(account, info).Compile();

        var ns = PacketTestUtilities.CreateTestNetState();
        ns.CityInfo = info;
        ns.Account = account;
        ns.ProtocolChanges = ProtocolChanges.Version70130;

        ns.SendCharacterList();

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestCharacterListOld()
    {
        var firstMobile = new Mobile((Serial)0x1);
        firstMobile.DefaultMobileInit();
        firstMobile.Name = "Test Mobile";

        var account = new MockedAccount
        {
            [0] = null,
            [1] = firstMobile,
            [2] = null,
            [3] = null,
            [4] = null
        };

        var info = new[]
        {
            new CityInfo("Test City", "Test Building", 50, 100, 10, -10)
        };

        var expected = new CharacterListOld(account, info).Compile();

        var ns = PacketTestUtilities.CreateTestNetState();
        ns.CityInfo = info;
        ns.Account = account;

        ns.SendCharacterList();

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestAccountLoginRej()
    {
        var reason = ALRReason.BadComm;
        var expected = new AccountLoginRej(reason).Compile();

        var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendAccountLoginRejected(reason);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestAccountLoginAck()
    {
        var info = new[]
        {
            new ServerInfo("Test Server", 0, TimeZoneInfo.Local, IPEndPoint.Parse("127.0.0.1"))
        };

        var expected = new AccountLoginAck(info).Compile();

        var ns = PacketTestUtilities.CreateTestNetState();
        ns.ServerInfo = info;

        ns.SendAccountLoginAck();

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestPlayServerAck()
    {
        var si = new ServerInfo("Test Server", 0, TimeZoneInfo.Local, IPEndPoint.Parse("127.0.0.1"));
        var authId = 0x123456;

        var expected = new PlayServerAck(si, authId).Compile();

        var ns = PacketTestUtilities.CreateTestNetState();

        ns.SendPlayServerAck(si, authId);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
    }
}
