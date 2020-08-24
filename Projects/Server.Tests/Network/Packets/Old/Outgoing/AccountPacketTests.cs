using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Server.Accounting;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
    public class AccountPacketTests : IClassFixture<ServerFixture>
    {
        internal class TestAccount : IAccount, IComparable<TestAccount>
        {
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
                if (TotalGold - amount < 0) return false;

                TotalGold -= amount;
                return true;
            }

            public bool WithdrawPlat(int amount)
            {
                if (TotalPlat - amount < 0) return false;

                TotalPlat -= amount;
                return true;
            }

            public long GetTotalGold() => TotalGold + TotalPlat * 100;

            public int CompareTo(TestAccount other) => other == null ? 1 : Username.CompareTo(other.Username);

            public int CompareTo(IAccount other) => other == null ? 1 : Username.CompareTo(other.Username);

            public string Username { get; set; }
            public string Email { get; set; }
            public AccessLevel AccessLevel { get; set; }
            public int Length { get; }
            public int Limit { get; }
            public int Count { get; }

            private Mobile[] m_Mobiles;
            private string m_Password;

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

            public TestAccount(Mobile[] mobiles)
            {
                m_Mobiles = mobiles;
                foreach (var mobile in mobiles)
                    if (mobile != null)
                        mobile.Account = this;

                Length = mobiles.Length;
                Count = mobiles.Count(t => t != null);
                Limit = mobiles.Length;
            }
        }

        internal class TestConnectionContext : ConnectionContext
        {
            public override string ConnectionId { get; set; }
            public override IFeatureCollection Features { get; }
            public override IDictionary<object, object> Items { get; set; }
            public override IDuplexPipe Transport { get; set; }
        }

        [Fact]
        public void TestChangeCharacter()
        {
            var firstMobile = new Mobile(0x1);
            firstMobile.DefaultMobileInit();
            firstMobile.Name = "Test Mobile";

            var account = new TestAccount(new[] { firstMobile, null, null });

            Span<byte> data = new ChangeCharacter(account).Compile();

            Span<byte> expectedData = stackalloc byte[5 + account.Length * 60];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0x81); // Packet ID
            expectedData.Write(ref pos, (ushort)expectedData.Length); // Length
            expectedData.Write(ref pos, (byte)1); // Count of non-null characters
            expectedData.Write(ref pos, (byte)0);

            for (var i = 0; i < account.Length; ++i)
            {
                Mobile m = account[i];
                if (m == null)
                {
#if NO_LOCAL_INIT
          expectedData.Clear(ref pos, 60);
#else
                    pos += 60;
#endif
                }
                else
                {
                    expectedData.WriteAsciiFixed(ref pos, m.Name, 30);
#if NO_LOCAL_INIT
          expectedData.Clear(ref pos, 30); // Password (empty)
#else
                    pos += 30;
#endif
                }
            }

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestClientVersionReq()
        {
            Span<byte> data = new ClientVersionReq().Compile();

            Span<byte> expectedData = stackalloc byte[]
            {
                0xBD, // Packet ID
                0x00,
                0x03 // Length
            };

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestDeleteResult()
        {
            Span<byte> data = new DeleteResult(DeleteResultType.BadRequest).Compile();

            Span<byte> expectedData = stackalloc byte[2];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0x85); // Packet ID
            expectedData.Write(ref pos, (byte)DeleteResultType.BadRequest);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestPopupMessage()
        {
            Span<byte> data = new PopupMessage(PMMessage.IdleWarning).Compile();

            Span<byte> expectedData = stackalloc byte[2];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0x53); // Packet ID
            expectedData.Write(ref pos, (byte)PMMessage.IdleWarning);

            AssertThat.Equal(data, expectedData);
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

            NetState ns = new NetState(new TestConnectionContext
            {
                RemoteEndPoint = IPEndPoint.Parse("127.0.0.1")
            })
            {
                Account = account,
                ProtocolChanges = protocolChanges
            };

            Span<byte> data = new SupportedFeatures(ns).Compile();

            Span<byte> expectedData = stackalloc byte[ns.ExtendedSupportedFeatures ? 5 : 3];
            int pos = 0;

            expectedData[pos++] = 0xB9; // Packet ID

            var flags = ExpansionInfo.GetFeatures(Expansion.EJ);

            if (ns.Account.Limit >= 6)
            {
                flags |= FeatureFlags.LiveAccount;
                flags &= ~FeatureFlags.UOTD;

                if (ns.Account.Limit > 6)
                    flags |= FeatureFlags.SeventhCharacterSlot;
                else
                    flags |= FeatureFlags.SixthCharacterSlot;
            }

            if (ns.ExtendedSupportedFeatures)
                expectedData.Write(ref pos, (uint)flags);
            else
                expectedData.Write(ref pos, (ushort)flags);

            AssertThat.Equal(data, expectedData);
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

            Span<byte> data = new LoginConfirm(m).Compile();

            Span<byte> expectedData = stackalloc byte[37];

            int pos = 0;
            expectedData.Write(ref pos, (byte)0x1B); // Packet ID
            expectedData.Write(ref pos, m.Serial);
#if NO_LOCAL_INIT
      expectedData.Write(ref pos, 0);
#else
            pos += 4;
#endif

            expectedData.Write(ref pos, (ushort)m.Body);
            expectedData.Write(ref pos, (ushort)m.X);
            expectedData.Write(ref pos, (ushort)m.Y);
            expectedData.Write(ref pos, (short)m.Z);
            expectedData.Write(ref pos, (byte)m.Direction);
#if NO_LOCAL_INIT
      expectedData.Write(ref pos, (byte)0);
#else
            pos++;
#endif
            expectedData.Write(ref pos, 0xFFFFFFFF);
#if NO_LOCAL_INIT
      expectedData.Write(ref pos, 0);
#else
            pos += 4;
#endif
            var map = m.Map;

            if (map == null || map == Map.Internal)
                map = m.LogoutMap;

            expectedData.Write(ref pos, (ushort)(map?.Width ?? Map.Felucca.Width));
            expectedData.Write(ref pos, (ushort)(map?.Height ?? Map.Felucca.Height));

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestLoginComplete()
        {
            Span<byte> data = new LoginComplete().Compile();

            Span<byte> expectedData = stackalloc byte[]
            {
                0x55 // Packet ID
            };

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestCharacterListUpdate()
        {
            var firstMobile = new Mobile(0x1);
            firstMobile.DefaultMobileInit();
            firstMobile.Name = "Test Mobile";

            var account = new TestAccount(new[] { firstMobile, null, null, null, null });

            Span<byte> data = new CharacterListUpdate(account).Compile();

            Span<byte> expectedData = stackalloc byte[4 + account.Length * 60];

            int pos = 0;
            expectedData.Write(ref pos, (byte)0x86); // Packet ID
            expectedData.Write(ref pos, (ushort)expectedData.Length); // Length

            int highSlot = -1;
            for (int i = account.Length - 1; i >= 0; i--)
                if (account[i] != null)
                {
                    highSlot = i;
                    break;
                }

            int count = Math.Max(Math.Max(highSlot + 1, account.Limit), 5);
            expectedData.Write(ref pos, (byte)count);

            for (int i = 0; i < count; i++)
            {
                var m = account[i];

                if (m != null)
                {
                    expectedData.WriteAsciiFixed(ref pos, m.Name, 30);
#if NO_LOCAL_INIT
          expectedData.Write(ref pos, (ulong)0);
          expectedData.Write(ref pos, (ulong)0);
          expectedData.Write(ref pos, (ulong)0);
          expectedData.Write(ref pos, 0);
          expectedData.Write(ref pos, (ushort)0);
#else
                    pos += 30;
#endif
                }
                else
                {
#if NO_LOCAL_INIT
          expectedData.Write(ref pos, (ulong)0);
          expectedData.Write(ref pos, (ulong)0);
          expectedData.Write(ref pos, (ulong)0);
          expectedData.Write(ref pos, (ulong)0);
          expectedData.Write(ref pos, (ulong)0);
          expectedData.Write(ref pos, (ulong)0);
          expectedData.Write(ref pos, (ulong)0);
          expectedData.Write(ref pos, 0);
#else
                    pos += 60;
#endif
                }
            }

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestCharacterList()
        {
            var firstMobile = new Mobile(0x1);
            firstMobile.DefaultMobileInit();
            firstMobile.Name = "Test Mobile";

            var account = new TestAccount(new[] { firstMobile, null, null, null, null });
            var info = new[]
            {
                new CityInfo("Test City", "Test Building", 50, 100, 10, -10)
            };

            Span<byte> data = new CharacterList(account, info).Compile();

            Span<byte> expectedData = stackalloc byte[11 + account.Length * 60 + info.Length * 89];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0xA9); // Packet ID
            expectedData.Write(ref pos, (ushort)expectedData.Length); // Length

            int highSlot = -1;
            for (int i = account.Length - 1; i >= 0; i--)
                if (account[i] != null)
                {
                    highSlot = i;
                    break;
                }

            int count = Math.Max(Math.Max(highSlot + 1, account.Limit), 5);
            expectedData.Write(ref pos, (byte)count);

            for (int i = 0; i < count; i++)
            {
                var m = account[i];
                if (m != null)
                {
                    expectedData.WriteAsciiFixed(ref pos, m.Name, 30);
#if NO_LOCAL_INIT
          expectedData.Write(ref pos, (ulong)0);
          expectedData.Write(ref pos, (ulong)0);
          expectedData.Write(ref pos, (ulong)0);
          expectedData.Write(ref pos, 0);
          expectedData.Write(ref pos, (ushort)0);
#else
                    pos += 30;
#endif
                }
                else
                {
#if NO_LOCAL_INIT
          expectedData.Write(ref pos, (ulong)0);
          expectedData.Write(ref pos, (ulong)0);
          expectedData.Write(ref pos, (ulong)0);
          expectedData.Write(ref pos, (ulong)0);
          expectedData.Write(ref pos, (ulong)0);
          expectedData.Write(ref pos, (ulong)0);
          expectedData.Write(ref pos, (ulong)0);
          expectedData.Write(ref pos, 0);
#else
                    pos += 60;
#endif
                }
            }

            expectedData.Write(ref pos, (byte)info.Length);

            for (int i = 0; i < info.Length; i++)
            {
                var ci = info[i];
                expectedData.Write(ref pos, (byte)i);
                expectedData.WriteAsciiFixed(ref pos, ci.City, 32);
                expectedData.WriteAsciiFixed(ref pos, ci.Building, 32);
                expectedData.Write(ref pos, ci.X);
                expectedData.Write(ref pos, ci.Y);
                expectedData.Write(ref pos, ci.Z);
                expectedData.Write(ref pos, ci.Map.MapID);
                expectedData.Write(ref pos, ci.Description);
#if NO_LOCAL_INIT
        expectedData.Write(ref pos, 0);
#else
                pos += 4;
#endif
            }

            var flags = ExpansionInfo.GetInfo(Expansion.EJ).CharacterListFlags;
            if (count > 6)
                flags |= CharacterListFlags.SeventhCharacterSlot |
                         CharacterListFlags.SixthCharacterSlot; // 7th Character Slot
            else if (count == 6)
                flags |= CharacterListFlags.SixthCharacterSlot; // 6th Character Slot
            else if (account.Limit == 1)
                flags |= CharacterListFlags.SlotLimit &
                         CharacterListFlags.OneCharacterSlot; // Limit Characters & One Character

            expectedData.Write(ref pos, (int)flags);
            expectedData.Write(ref pos, (short)-1);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestCharacterListOld()
        {
            var firstMobile = new Mobile(0x1);
            firstMobile.DefaultMobileInit();
            firstMobile.Name = "Test Mobile";

            var account = new TestAccount(new[] { firstMobile, null, null, null, null });
            var info = new[]
            {
                new CityInfo("Test City", "Test Building", 50, 100, 10, -10)
            };

            Span<byte> data = new CharacterListOld(account, info).Compile();

            Span<byte> expectedData = stackalloc byte[9 + account.Length * 60 + info.Length * 63];

            int pos = 0;
            expectedData.Write(ref pos, (byte)0xA9); // Packet ID
            expectedData.Write(ref pos, (ushort)expectedData.Length); // Length

            int highSlot = -1;
            for (int i = account.Length - 1; i >= 0; i--)
                if (account[i] != null)
                {
                    highSlot = i;
                    break;
                }

            int count = Math.Max(Math.Max(highSlot + 1, account.Limit), 5);
            expectedData.Write(ref pos, (byte)count);

            for (int i = 0; i < count; i++)
            {
                var m = account[i];
                if (m != null)
                {
                    expectedData.WriteAsciiFixed(ref pos, m.Name, 30);
#if NO_LOCAL_INIT
          expectedData.Write(ref pos, (ulong)0);
          expectedData.Write(ref pos, (ulong)0);
          expectedData.Write(ref pos, (ulong)0);
          expectedData.Write(ref pos, 0);
          expectedData.Write(ref pos, (ushort)0);
#else
                    pos += 30;
#endif
                }
                else
                {
#if NO_LOCAL_INIT
          expectedData.Write(ref pos, (ulong)0);
          expectedData.Write(ref pos, (ulong)0);
          expectedData.Write(ref pos, (ulong)0);
          expectedData.Write(ref pos, (ulong)0);
          expectedData.Write(ref pos, (ulong)0);
          expectedData.Write(ref pos, (ulong)0);
          expectedData.Write(ref pos, (ulong)0);
          expectedData.Write(ref pos, 0);
#else
                    pos += 60;
#endif
                }
            }

            expectedData.Write(ref pos, (byte)info.Length);

            for (int i = 0; i < info.Length; i++)
            {
                var ci = info[i];
                expectedData.Write(ref pos, (byte)i);
                expectedData.WriteAsciiFixed(ref pos, ci.City, 31);
                expectedData.WriteAsciiFixed(ref pos, ci.Building, 31);
            }

            var flags = ExpansionInfo.GetInfo(Expansion.EJ).CharacterListFlags;
            if (count > 6)
                flags |= CharacterListFlags.SeventhCharacterSlot |
                         CharacterListFlags.SixthCharacterSlot; // 7th Character Slot
            else if (count == 6)
                flags |= CharacterListFlags.SixthCharacterSlot; // 6th Character Slot
            else if (account.Limit == 1)
                flags |= CharacterListFlags.SlotLimit &
                         CharacterListFlags.OneCharacterSlot; // Limit Characters & One Character

            expectedData.Write(ref pos, (int)flags);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestAccountLoginRej()
        {
            var reason = ALRReason.BadComm;
            Span<byte> data = new AccountLoginRej(reason).Compile();

            Span<byte> expectedData = stackalloc byte[2];
            int pos = 0;

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

            Span<byte> data = new AccountLoginAck(info).Compile();

            Span<byte> expectedData = stackalloc byte[6 + info.Length * 40];

            int pos = 0;
            expectedData.Write(ref pos, (byte)0xA8); // Packet ID
            expectedData.Write(ref pos, (ushort)expectedData.Length);
            expectedData.Write(ref pos, (byte)0x5D); // Unknown
            expectedData.Write(ref pos, (ushort)info.Length);

            for (int i = 0; i < info.Length; i++)
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

            Span<byte> data = new PlayServerAck(si).Compile();

            var addr = Utility.GetAddressValue(si.Address.Address);

            Span<byte> expectedData = stackalloc byte[11];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0x8C); // Packet ID
            expectedData.WriteLE(ref pos, addr);
            expectedData.Write(ref pos, (ushort)si.Address.Port);
            expectedData.Write(ref pos, -1); // Auth ID

            AssertThat.Equal(data, expectedData);
        }
    }
}
