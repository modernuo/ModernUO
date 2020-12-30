using System;
using System.Buffers;
using Server.Network;
using Xunit;

namespace Server.Tests.Network
{
    public class PlayerPacketTests : IClassFixture<ServerFixture>
    {
        [Theory]
        [InlineData(StatLockType.Down, StatLockType.Up, StatLockType.Locked)]
        public void TestStatLockInfo(StatLockType str, StatLockType intel, StatLockType dex)
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();
            m.StrLock = str;
            m.IntLock = intel;
            m.DexLock = dex;

            var expected = new StatLockInfo(m).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendStatLockInfo(m);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(200)]
        public void TestChangeUpdateRange(byte range)
        {
            var expected = new ChangeUpdateRange(range).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendChangeUpdateRange(range);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestDeathStatus(bool dead)
        {
            var expected = new DeathStatus(dead).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendDeathStatus(dead);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(0, false)]
        [InlineData(100, true)]
        [InlineData(1000, false)]
        public void TestSpecialAbility(int abilityId, bool active)
        {
            var expected = new ToggleSpecialAbility(abilityId, active).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendToggleSpecialAbility(abilityId, active);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Theory]
        [InlineData(0x1000u, "This is a header", "This is a body", "This is a footer")]
        [InlineData(0x1000u, null, null, null)]
        public void TestDisplayProfile(Serial m, string header, string body, string footer)
        {
            var expected = new DisplayProfile(m, header, body, footer).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendDisplayProfile(m, header, body, footer);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Theory]
        [InlineData(LRReason.CannotLift)]
        [InlineData(LRReason.TryToSteal)]
        public void TestLiftRej(LRReason reason)
        {
            var expected = new LiftRej(reason).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendLiftReject(reason);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestLogoutAck()
        {
            var expected = new LogoutAck().Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendLogoutAck();

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Theory]
        [InlineData(1, 2, 3)]
        [InlineData(4, 5, 6)]
        [InlineData(0x1234, 0x5678, 0x9ABC)]
        public void TestWeather(int type, int density, int temp)
        {
            var expected = new Weather(type, density, temp).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendWeather((byte)type, (byte)density, (byte)temp);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestServerChange()
        {
            var p = new Point3D(100, 1000, 1);
            var map = Map.Felucca;
            var data = new ServerChange(p, map).Compile();

            Span<byte> expectedData = stackalloc byte[16];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x76); // Packet ID
            expectedData.Write(ref pos, (ushort)p.X);
            expectedData.Write(ref pos, (ushort)p.Y);
            expectedData.Write(ref pos, (short)p.Z);
#if NO_LOCAL_INIT
            expectedData.Write(ref pos, (byte)0); // Unknown
            expectedData.Write(ref pos, 0); // Server X, Server Y
#else
            pos += 5;
#endif
            expectedData.Write(ref pos, (ushort)map.Width);  // Server Width
            expectedData.Write(ref pos, (ushort)map.Height); // Server Height

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestSkillUpdate()
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            var skills = m.Skills;
            m.Skills[SkillName.Alchemy].BaseFixedPoint = 1000; // GM Alchemy

            var data = new SkillUpdate(skills).Compile();

            var length = 6 + skills.Length * 9;
            Span<byte> expectedData = stackalloc byte[length];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x3A);     // Packet ID
            expectedData.Write(ref pos, (ushort)length); // Length
            expectedData.Write(ref pos, (byte)0x02);     // type: absolute, capped

            for (var i = 0; i < skills.Length; i++)
            {
                var s = skills[i];

                var v = s.NonRacialValue;
                var uv = Math.Clamp((int)(v * 10), 0, 0xFFFF);

                expectedData.Write(ref pos, (ushort)(s.Info.SkillID + 1));
                expectedData.Write(ref pos, (ushort)uv);
                expectedData.Write(ref pos, (ushort)s.BaseFixedPoint);
                expectedData.Write(ref pos, (byte)s.Lock);
                expectedData.Write(ref pos, (ushort)s.CapFixedPoint);
            }

            AssertThat.Equal(data, expectedData);
        }

        [Theory, InlineData(0), InlineData(10), InlineData(255)]
        public void TestSequence(byte num)
        {
            var data = new Sequence(num).Compile();

            Span<byte> expectedData = stackalloc byte[2];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x7B); // Packet ID
            expectedData.Write(ref pos, num);

            AssertThat.Equal(data, expectedData);
        }

        [Theory, InlineData(SkillName.Alchemy, 0, 1), InlineData(SkillName.Archery, 10, 1000),
         InlineData(SkillName.Begging, 100000, 1000)]
        public void TestSkillChange(SkillName skillName, int baseFixedPoint, int capFixedPoint)
        {
            // TODO: Eliminate all of this and just create a Skill directly
            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            var skill = m.Skills[skillName];
            skill.BaseFixedPoint = baseFixedPoint;
            skill.CapFixedPoint = capFixedPoint;

            var data = new SkillChange(skill).Compile();

            Span<byte> expectedData = stackalloc byte[13];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x3A); // Packet ID
            expectedData.Write(ref pos, (ushort)13); // Length
            expectedData.Write(ref pos, (byte)0xDF); // type: delta, capped

            var v = skill.NonRacialValue;
            var uv = Math.Clamp((int)(v * 10), 0, 0xFFFF);

            expectedData.Write(ref pos, (ushort)skill.Info.SkillID);
            expectedData.Write(ref pos, (ushort)uv);
            expectedData.Write(ref pos, (ushort)skill.BaseFixedPoint);
            expectedData.Write(ref pos, (byte)skill.Lock);
            expectedData.Write(ref pos, (ushort)skill.CapFixedPoint);

            AssertThat.Equal(data, expectedData);
        }

        [Theory, InlineData(null), InlineData(""), InlineData("This is a URL, I promise")]
        public void TestLaunchBrowser(string url)
        {
            var data = new LaunchBrowser(url).Compile();

            url ??= "";

            var length = 4 + url.Length;
            Span<byte> expectedData = stackalloc byte[length];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0xA5);     // Packet ID
            expectedData.Write(ref pos, (ushort)length); // Length
            expectedData.WriteAsciiNull(ref pos, url);   // Note: use punycode for unicode URLs

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestDragEffect()
        {
            var src = new Entity(0x1, new Point3D(1000, 100, 10), Map.Felucca);
            var targ = new Entity(0x2, new Point3D(1125, 125, 5), Map.Felucca);
            var itemID = 0x384;
            var hue = 1024;
            var amount = 25;

            var data = new DragEffect(src, targ, itemID, hue, amount).Compile();

            Span<byte> expectedData = stackalloc byte[26];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x23); // Packet ID
            expectedData.Write(ref pos, (ushort)itemID);

#if NO_LOCAL_INIT
            expectedData.Write(ref pos, (byte)0);
#else
            pos++;
#endif

            expectedData.Write(ref pos, (ushort)hue);
            expectedData.Write(ref pos, (ushort)amount);
            expectedData.Write(ref pos, src.Serial);
            expectedData.Write(ref pos, src.Location);
            expectedData.Write(ref pos, targ.Serial);
            expectedData.Write(ref pos, targ.Location);

            AssertThat.Equal(data, expectedData);
        }

        [Theory, InlineData(1, false), InlineData(2, true)]
        public void TestSeasonChange(int season, bool playSound)
        {
            var data = new SeasonChange(season, playSound).Compile();

            Span<byte> expectedData = stackalloc byte[3];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0xBC); // Packet ID
            expectedData.Write(ref pos, (byte)season);
            expectedData.Write(ref pos, playSound);

            AssertThat.Equal(data, expectedData);
        }

        [Theory, InlineData(0x1024, "Test Title", true, true), InlineData(0x1024, "Test Title", false, true),
         InlineData(0x1024, "Test Title", true, false)]
        public void TestDisplayPaperdoll(uint m, string title, bool warmode, bool canLift)
        {
            var data = new DisplayPaperdoll(m, title, warmode, canLift).Compile();

            Span<byte> expectedData = stackalloc byte[66];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x88); // Packet ID
            expectedData.Write(ref pos, m);
            expectedData.WriteAsciiFixed(ref pos, title, 60);
            byte flags = 0x00;
            if (warmode)
            {
                flags |= 0x01;
            }

            if (canLift)
            {
                flags |= 0x02;
            }

            expectedData.Write(ref pos, flags);

            AssertThat.Equal(data, expectedData);
        }

        [Theory, InlineData(MusicName.Approach), InlineData(MusicName.Combat1), InlineData(MusicName.ValoriaShips)]
        public void TestPlayMusic(MusicName music)
        {
            var data = new PlayMusic(music).Compile();

            Span<byte> expectedData = stackalloc byte[3];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x6D);    // Packet ID
            expectedData.Write(ref pos, (ushort)music); // Flags

            AssertThat.Equal(data, expectedData);
        }

        [Theory, InlineData(10, 1, "Some text"), InlineData(100, 10, "Some more text")]
        public void TestScrollMessage(int type, int tip, string text)
        {
            var data = new ScrollMessage(type, tip, text).Compile();

            text ??= "";
            var length = 10 + text.Length;
            Span<byte> expectedData = stackalloc byte[length];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0xA6);     // Packet ID
            expectedData.Write(ref pos, (ushort)length); // Length
            expectedData.Write(ref pos, (byte)type);
            expectedData.Write(ref pos, tip);
            expectedData.Write(ref pos, (ushort)text.Length);
            expectedData.WriteAscii(ref pos, text);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestCurrentTime()
        {
            var date = DateTime.Parse("2020-01-01 14:10:05");

            var data = new CurrentTime(date).Compile();

            Span<byte> expectedData = stackalloc byte[4];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x5B); // Packet ID
            expectedData.Write(ref pos, (byte)date.Hour);
            expectedData.Write(ref pos, (byte)date.Minute);
            expectedData.Write(ref pos, (byte)date.Second);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestPathfindMessage()
        {
            var p = new Point3D(1000, 10, 1);
            var data = new PathfindMessage(p).Compile();

            Span<byte> expectedData = stackalloc byte[7];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x38); // Packet ID
            expectedData.Write(ref pos, (ushort)p.X);
            expectedData.Write(ref pos, (ushort)p.Y);
            expectedData.Write(ref pos, (short)p.Z);

            AssertThat.Equal(data, expectedData);
        }

        [Theory, InlineData(0), InlineData(10), InlineData(100)]
        public void TestPingAck(byte ping)
        {
            var data = new PingAck(ping).Compile();

            Span<byte> expectedData = stackalloc byte[2];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x73); // Packet ID
            expectedData.Write(ref pos, ping);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestClearAbility()
        {
            var data = new ClearWeaponAbility().Compile();

            Span<byte> expectedData = stackalloc byte[5];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0xBF);   // Packet ID
            expectedData.Write(ref pos, (ushort)5);    // Length
            expectedData.Write(ref pos, (ushort)0x21); // Sub-packet

            AssertThat.Equal(data, expectedData);
        }
    }
}
