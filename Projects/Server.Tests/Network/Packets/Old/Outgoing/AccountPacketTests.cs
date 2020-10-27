using System;
using System.Net;
using Server.Network;
using Xunit;

namespace Server.Tests.Network
{
    public class AccountPacketTests : IClassFixture<ServerFixture>
    {
        [Fact]
        public void TestChangeCharacter()
        {
            var firstMobile = new Mobile(0x1);
            firstMobile.DefaultMobileInit();
            firstMobile.RawName = "Test Mobile";

            var secondMobile = new Mobile(0x2);
            secondMobile.DefaultMobileInit();
            secondMobile.RawName = null;

            var account = new MockAccount(new[] { firstMobile, null, secondMobile });
            var expected = new ChangeCharacter(account).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            Packets.SendChangeCharacter(ns, account);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestClientVersionReq()
        {
            var expected = new ClientVersionReq().Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();

            Packets.SendClientVersionRequest(ns);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestDeleteResult()
        {
            var expected = new DeleteResult(DeleteResultType.BadRequest).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            Packets.SendCharacterDeleteResult(ns, DeleteResultType.BadRequest);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestPopupMessage()
        {
            var expected = new PopupMessage(PMMessage.LoginSyncError).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            Packets.SendPopupMessage(ns, PMMessage.LoginSyncError);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Theory]
        [InlineData(ProtocolChanges.Version70610)]
        [InlineData(ProtocolChanges.Version6000)]
        public void TestSupportedFeatures(ProtocolChanges protocolChanges)
        {
            var firstMobile = new Mobile(0x1);
            firstMobile.DefaultMobileInit();
            firstMobile.Name = "Test Mobile";

            var account = new MockAccount(new[] { firstMobile, null, null, null, null });

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.Account = account;
            ns.ProtocolChanges = protocolChanges;

            var expected = new SupportedFeatures(ns).Compile();
            Packets.SendSupportedFeature(ns);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestLoginConfirm()
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();
            m.Body = 0x100;
            m.X = 100;
            m.Y = 10;
            m.Z = -10;
            m.Direction = Direction.Down;
            m.LogoutMap = Map.Felucca;

            var expected = new LoginConfirm(m).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            Packets.SendLoginConfirmation(ns, m);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestLoginComplete()
        {
            var expected = new LoginComplete().Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            Packets.SendLoginComplete(ns);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestCharacterListUpdate()
        {
            var firstMobile = new Mobile(0x1);
            firstMobile.DefaultMobileInit();
            firstMobile.RawName = "Test Mobile";

            var acct = new MockAccount(new[] { null, firstMobile, null, null, null });

            var expected = new CharacterListUpdate(acct).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            Packets.SendCharacterListUpdate(ns, acct);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestCharacterList70130()
        {
            var firstMobile = new Mobile(0x1);
            firstMobile.DefaultMobileInit();
            firstMobile.Name = "Test Mobile";

            var acct = new MockAccount(new[] { null, firstMobile, null, null, null });
            var info = new[]
            {
                new CityInfo("Test City", "Test Building", 50, 100, 10, -10)
            };

            var expected = new CharacterList(acct, info).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.CityInfo = info;
            ns.Account = acct;
            ns.ProtocolChanges = ProtocolChanges.Version70130;

            Packets.SendCharacterList(ns);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestCharacterListOld()
        {
            var firstMobile = new Mobile(0x1);
            firstMobile.DefaultMobileInit();
            firstMobile.Name = "Test Mobile";

            var acct = new MockAccount(new[] { null, firstMobile, null, null, null });
            var info = new[]
            {
                new CityInfo("Test City", "Test Building", 50, 100, 10, -10)
            };

            var expected = new CharacterListOld(acct, info).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.CityInfo = info;
            ns.Account = acct;

            Packets.SendCharacterList(ns);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestAccountLoginRej()
        {
            var reason = ALRReason.BadComm;
            var expected = new AccountLoginRej(reason).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            Packets.SendAccountLoginRejected(ns, reason);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestAccountLoginAck()
        {
            var info = new[]
            {
                new ServerInfo("Test Server", 0, TimeZoneInfo.Local, IPEndPoint.Parse("127.0.0.1"))
            };

            var expected = new AccountLoginAck(info).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.ServerInfo = info;

            Packets.SendAccountLoginAck(ns);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestPlayServerAck()
        {
            var si = new ServerInfo("Test Server", 0, TimeZoneInfo.Local, IPEndPoint.Parse("127.0.0.1"));
            var authId = 0x123456;

            var expected = new PlayServerAck(si, authId).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();

            Packets.SendPlayServerAck(ns, si, authId);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }
    }
}
