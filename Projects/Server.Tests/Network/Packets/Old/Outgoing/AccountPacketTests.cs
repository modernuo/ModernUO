using System;
using System.Buffers;
using System.Linq;
using System.Net;
using Server.Accounting;
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

            var account = new TestAccount(new[] { firstMobile, null, secondMobile });
            var oldPacket = new ChangeCharacter(account).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            Packets.SendChangeCharacter(ns, account);

            ns.SendPipe.Reader.TryRead(out var buffer);
            AssertThat.Equal(buffer.GetSpan(0), oldPacket);
        }

        [Fact]
        public void TestClientVersionReq()
        {
            var expected = new ClientVersionReq().Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();

            Packets.SendClientVersionRequest(ns);

            ns.SendPipe.Reader.TryRead(out var buffer);
            AssertThat.Equal(buffer.GetSpan(0), expected);
        }

        [Fact]
        public void TestDeleteResult()
        {
            var expected = new DeleteResult(DeleteResultType.BadRequest).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            Packets.SendCharacterDeleteResult(ns, DeleteResultType.BadRequest);

            ns.SendPipe.Reader.TryRead(out var buffer);
            AssertThat.Equal(buffer.GetSpan(0), expected);
        }

        [Fact]
        public void TestPopupMessage()
        {
            var expected = new PopupMessage(PMMessage.LoginSyncError).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            Packets.SendPopupMessage(ns, PMMessage.LoginSyncError);

            ns.SendPipe.Reader.TryRead(out var buffer);
            AssertThat.Equal(buffer.GetSpan(0), expected);
        }

        [Theory]
        [InlineData(ProtocolChanges.Version70610)]
        [InlineData(ProtocolChanges.Version6000)]
        public void TestSupportedFeatures(ProtocolChanges protocolChanges)
        {
            var firstMobile = new Mobile(0x1);
            firstMobile.DefaultMobileInit();
            firstMobile.Name = "Test Mobile";

            var account = new TestAccount(new[] { firstMobile, null, null, null, null });

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.Account = account;
            ns.ProtocolChanges = protocolChanges;

            var expected = new SupportedFeatures(ns).Compile();
            Packets.SendSupportedFeature(ns);

            ns.SendPipe.Reader.TryRead(out var buffer);
            AssertThat.Equal(buffer.GetSpan(0), expected);
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

            ns.SendPipe.Reader.TryRead(out var buffer);
            AssertThat.Equal(buffer.GetSpan(0), expected);
        }

        [Fact]
        public void TestLoginComplete()
        {
            var expected = new LoginComplete().Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            Packets.SendLoginComplete(ns);

            ns.SendPipe.Reader.TryRead(out var buffer);
            AssertThat.Equal(buffer.GetSpan(0), expected);
        }

        [Fact]
        public void TestCharacterListUpdate()
        {
            var firstMobile = new Mobile(0x1);
            firstMobile.DefaultMobileInit();
            firstMobile.RawName = "Test Mobile";

            var acct = new TestAccount(new[] { null, firstMobile, null, null, null });

            var expected = new CharacterListUpdate(acct).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            Packets.SendCharacterListUpdate(ns, acct);

            ns.SendPipe.Reader.TryRead(out var buffer);
            AssertThat.Equal(buffer.GetSpan(0), expected);
        }

        [Fact]
        public void TestCharacterList70130()
        {
            var firstMobile = new Mobile(0x1);
            firstMobile.DefaultMobileInit();
            firstMobile.Name = "Test Mobile";

            var acct = new TestAccount(new[] { null, firstMobile, null, null, null });
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

            ns.SendPipe.Reader.TryRead(out var buffer);
            AssertThat.Equal(buffer.GetSpan(0), expected);
        }

        [Fact]
        public void TestCharacterListOld()
        {
            var firstMobile = new Mobile(0x1);
            firstMobile.DefaultMobileInit();
            firstMobile.Name = "Test Mobile";

            var acct = new TestAccount(new[] { null, firstMobile, null, null, null });
            var info = new[]
            {
                new CityInfo("Test City", "Test Building", 50, 100, 10, -10)
            };

            var expected = new CharacterListOld(acct, info).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.CityInfo = info;
            ns.Account = acct;

            Packets.SendCharacterList(ns);

            ns.SendPipe.Reader.TryRead(out var buffer);
            AssertThat.Equal(buffer.GetSpan(0), expected);
        }

        [Fact]
        public void TestAccountLoginRej()
        {
            var reason = ALRReason.BadComm;
            var data = new AccountLoginRej(reason).Compile();

            Span<byte> expectedData = stackalloc byte[2];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x82); // Packet ID
            expectedData.Write(ref pos, (byte)reason);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestAccountLoginAck()
        {
            var info = new[]
            {
                new ServerInfo("Test Server", 0, TimeZoneInfo.Local, IPEndPoint.Parse("127.0.0.1"))
            };

            var data = new AccountLoginAck(info).Compile();

            Span<byte> expectedData = stackalloc byte[6 + info.Length * 40];

            var pos = 0;
            expectedData.Write(ref pos, (byte)0xA8); // Packet ID
            expectedData.Write(ref pos, (ushort)expectedData.Length);
            expectedData.Write(ref pos, (byte)0x5D); // Unknown
            expectedData.Write(ref pos, (ushort)info.Length);

            for (var i = 0; i < info.Length; i++)
            {
                var si = info[i];
                expectedData.Write(ref pos, (ushort)i);
                expectedData.WriteAsciiFixed(ref pos, si.Name, 32);
                expectedData.Write(ref pos, (byte)si.FullPercent);
                expectedData.Write(ref pos, (byte)si.TimeZone);
                expectedData.Write(ref pos, Utility.GetAddressValue(si.Address.Address));
            }

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestPlayServerAck()
        {
            var si = new ServerInfo("Test Server", 0, TimeZoneInfo.Local, IPEndPoint.Parse("127.0.0.1"));

            var data = new PlayServerAck(si).Compile();

            var addr = Utility.GetAddressValue(si.Address.Address);

            Span<byte> expectedData = stackalloc byte[11];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x8C); // Packet ID
            expectedData.WriteLE(ref pos, addr);
            expectedData.Write(ref pos, (ushort)si.Address.Port);
            expectedData.Write(ref pos, -1); // Auth ID

            AssertThat.Equal(data, expectedData);
        }

        internal class TestAccount : IAccount, IComparable<TestAccount>
        {
            private readonly Mobile[] m_Mobiles;
            private string m_Password;

            public TestAccount(Mobile[] mobiles)
            {
                m_Mobiles = mobiles;
                foreach (var mobile in mobiles)
                {
                    if (mobile != null)
                    {
                        mobile.Account = this;
                    }
                }

                Length = mobiles.Length;
                Count = mobiles.Count(t => t != null);
                Limit = mobiles.Length;
            }

            public int TotalGold { get; private set; }
            public int TotalPlat { get; private set; }

            public bool DepositGold(int amount)
            {
                TotalGold += amount;
                return true;
            }

            public bool DepositPlat(int amount)
            {
                TotalPlat += amount;
                return true;
            }

            public bool WithdrawGold(int amount)
            {
                if (TotalGold - amount < 0)
                {
                    return false;
                }

                TotalGold -= amount;
                return true;
            }

            public bool WithdrawPlat(int amount)
            {
                if (TotalPlat - amount < 0)
                {
                    return false;
                }

                TotalPlat -= amount;
                return true;
            }

            public long GetTotalGold() => TotalGold + TotalPlat * 100;

            public int CompareTo(IAccount other) => other == null ? 1 : Username.CompareTo(other.Username);

            public string Username { get; set; }
            public string Email { get; set; }
            public AccessLevel AccessLevel { get; set; }
            public int Length { get; }
            public int Limit { get; }
            public int Count { get; }

            public Mobile this[int index]
            {
                get => m_Mobiles[index];
                set => m_Mobiles[index] = value;
            }

            public void Delete()
            {
            }

            public void SetPassword(string password)
            {
                m_Password = password;
            }

            public bool CheckPassword(string password) => m_Password == password;

            public int CompareTo(TestAccount other) => other == null ? 1 : Username.CompareTo(other.Username);
        }
    }
}
