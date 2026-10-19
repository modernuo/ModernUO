using System;
using System.Net;
using Server.Network;
using Xunit;

namespace Server.Tests.Network;

[Collection("Sequential Server Tests")]
public class AccountPacketTests
{
    [Fact]
    public void TestChangeCharacter()
    {
        var firstMobile = new Mobile((Serial)0x1);
        firstMobile.DefaultMobileInit();
        firstMobile.RawName = "Test Mobile";

        var secondMobile = new Mobile((Serial)0x2);
        secondMobile.DefaultMobileInit();
        secondMobile.RawName = null;

        var account = new MockAccount
        {
            [0] = firstMobile,
            [1] = null,
            [2] = secondMobile
        };

        PacketAssert.Sends(new ChangeCharacter(account), ns => ns.SendChangeCharacter(account));
    }

    [Fact]
    public void TestClientVersionReq()
    {
        PacketAssert.Sends(new ClientVersionReq(), ns => ns.SendClientVersionRequest());
    }

    [Fact]
    public void TestDeleteResult()
    {
        PacketAssert.Sends(
            new DeleteResult(DeleteResultType.BadRequest),
            ns => ns.SendCharacterDeleteResult(DeleteResultType.BadRequest)
        );
    }

    [Fact]
    public void TestPopupMessage()
    {
        PacketAssert.Sends(
            new PopupMessage(PMMessage.LoginSyncError),
            ns => ns.SendPopupMessage(PMMessage.LoginSyncError)
        );
    }

    [Theory, InlineData(ProtocolChanges.Version70610), InlineData(ProtocolChanges.Version6000)]
    public void TestSupportedFeatures(ProtocolChanges protocolChanges)
    {
        var firstMobile = new Mobile((Serial)0x1);
        firstMobile.DefaultMobileInit();
        firstMobile.Name = "Test Mobile";

        var account = new MockAccount
        {
            [0] = firstMobile,
            [1] = null,
            [2] = null,
            [3] = null,
            [4] = null
        };

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.Account = account;
        ns.ProtocolChanges = protocolChanges;

        var expected = new SupportedFeatures(ns).Compile();
        ns.SendSupportedFeature();

        var result = ns.SendBuffer.GetReadSpan();
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

        PacketAssert.Sends(new LoginConfirm(m), ns => ns.SendLoginConfirmation(m));
    }

    [Fact]
    public void TestLoginComplete()
    {
        PacketAssert.Sends(new LoginComplete(), ns => ns.SendLoginComplete());
    }

    [Fact]
    public void TestCharacterListUpdate()
    {
        var firstMobile = new Mobile((Serial)0x1);
        firstMobile.DefaultMobileInit();
        firstMobile.RawName = "Test Mobile";

        var account = new MockAccount
        {
            [0] = null,
            [1] = firstMobile,
            [2] = null,
            [3] = null,
            [4] = null
        };

        PacketAssert.Sends(new CharacterListUpdate(account), ns => ns.SendCharacterListUpdate(account));
    }

    [Fact]
    public void TestCharacterList70130()
    {
        var firstMobile = new Mobile((Serial)0x1);
        firstMobile.DefaultMobileInit();
        firstMobile.Name = "Test Mobile";

        var account = new MockAccount
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

        PacketAssert.Sends(
            new CharacterList(account, info),
            ns =>
            {
                ns.CityInfo = info;
                ns.Account = account;
                ns.SendCharacterList();
            },
            ProtocolChanges.Version70130
        );
    }

    [Fact]
    public void TestCharacterListOld()
    {
        var firstMobile = new Mobile((Serial)0x1);
        firstMobile.DefaultMobileInit();
        firstMobile.Name = "Test Mobile";

        var account = new MockAccount
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

        PacketAssert.Sends(
            new CharacterListOld(account, info),
            ns =>
            {
                ns.CityInfo = info;
                ns.Account = account;
                ns.SendCharacterList();
            }
        );
    }

    [Fact]
    public void TestAccountLoginRej()
    {
        var reason = ALRReason.BadComm;

        PacketAssert.Sends(new AccountLoginRej(reason), ns => ns.SendAccountLoginRejected(reason));
    }

    [Fact]
    public void TestAccountLoginAck()
    {
        var info = new[]
        {
            new ServerInfo("Test Server", 0, TimeZoneInfo.Local, IPEndPoint.Parse("127.0.0.1"))
        };

        PacketAssert.Sends(
            new AccountLoginAck(info),
            ns =>
            {
                ns.ServerInfo = info;
                ns.SendAccountLoginAck();
            }
        );
    }

    [Fact]
    public void TestPlayServerAck()
    {
        var si = new ServerInfo("Test Server", 0, TimeZoneInfo.Local, IPEndPoint.Parse("127.0.0.1"));
        const int authId = 0x123456;

        PacketAssert.Sends(new PlayServerAck(si, authId), ns => ns.SendPlayServerAck(si, authId));
    }
}
