using System;
using Server.HuePickers;
using Server.Network;
using Xunit;

namespace Server.Tests.Network
{
    [Collection("Sequential Tests")]
    public class PlayerPacketTests : IClassFixture<ServerFixture>
    {
        [Theory]
        [InlineData(StatLockType.Down, StatLockType.Up, StatLockType.Locked)]
        public void TestStatLockInfo(StatLockType str, StatLockType intel, StatLockType dex)
        {
            var m = new Mobile((Serial)0x1);
            m.DefaultMobileInit();
            m.StrLock = str;
            m.IntLock = intel;
            m.DexLock = dex;

            var expected = new StatLockInfo(m).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendStatLockInfo(m);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(200)]
        public void TestChangeUpdateRange(int range)
        {
            var expected = new ChangeUpdateRange(range).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendChangeUpdateRange((byte)range);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestDeathStatus(bool dead)
        {
            var expected = new DeathStatus(dead).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendDeathStatus(dead);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Theory]
        [InlineData(0x1000u, "This is a header", "This is a body", "This is a footer")]
        [InlineData(0x1000u, null, null, null)]
        public void TestDisplayProfile(uint serial, string header, string body, string footer)
        {
            var expected = new DisplayProfile((Serial)serial, header, body, footer).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendDisplayProfile((Serial)serial, header, body, footer);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Theory]
        [InlineData(LRReason.CannotLift)]
        [InlineData(LRReason.TryToSteal)]
        public void TestLiftRej(LRReason reason)
        {
            var expected = new LiftRej(reason).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendLiftReject(reason);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Fact]
        public void TestLogoutAck()
        {
            var expected = new LogoutAck().Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendLogoutAck();

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Theory]
        [InlineData(1, 2, 3)]
        [InlineData(4, 5, 6)]
        [InlineData(0x1234, 0x5678, 0x9ABC)]
        public void TestWeather(int type, int density, int temp)
        {
            var expected = new Weather(type, density, temp).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendWeather((byte)type, (byte)density, (byte)temp);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Theory]
        [InlineData(100, 1000, 1, 0)]
        public void TestServerChange(int x, int y, int z, int mapID)
        {
            var p = new Point3D(x, y, z);
            var map = Map.Maps[mapID];
            var expected = new ServerChange(p, map).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendServerChange(p, map);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(255)]
        public void TestSequence(byte num)
        {
            var expected = new Sequence(num).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendSequence(num);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("This is a URL, I promise")]
        [InlineData("https://www.modernuo.com")]
        public void TestLaunchBrowser(string uri)
        {
            var expected = new LaunchBrowser(uri).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendLaunchBrowser(uri);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Theory]
        [InlineData(0x1000u, 1000, 100, 10, 0x2000u, 1125, 125, 5, 0x384, 1024, 25)]
        public void TestDragEffect(
            uint srcSerial, int srcX, int srcY, int srcZ,
            uint trgSerial, int trgX, int trgY, int trgZ,
            int itemId, int hue, int amount
        )
        {
            var src = new Entity((Serial)srcSerial, new Point3D(srcX, srcY, srcZ), null);
            var targ = new Entity((Serial)trgSerial, new Point3D(trgX, trgY, trgZ), null);

            var expected = new DragEffect(src, targ, itemId, hue, amount).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendDragEffect(
                src.Serial, src.Location,
                targ.Serial, targ.Location,
                itemId, hue, amount
            );

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Theory]
        [InlineData(1, false)]
        [InlineData(2, true)]
        public void TestSeasonChange(int season, bool playSound)
        {
            var expected = new SeasonChange(season, playSound).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendSeasonChange((byte)season, playSound);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Theory]
        [InlineData(0x1024u, "Test Title", true, true)]
        [InlineData(0x1024u, "Test Title", false, true)]
        [InlineData(0x1024u, "Test Title", true, false)]
        public void TestDisplayPaperdoll(uint m, string title, bool warmode, bool canLift)
        {
            var expected = new DisplayPaperdoll((Serial)m, title, warmode, canLift).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendDisplayPaperdoll((Serial)m, title, warmode, canLift);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Theory]
        [InlineData(MusicName.Approach)]
        [InlineData(MusicName.Combat1)]
        [InlineData(MusicName.ValoriaShips)]
        public void TestPlayMusic(MusicName music)
        {
            var expected = new PlayMusic(music).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendPlayMusic(music);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Theory]
        [InlineData(10, 1, "Some text")]
        [InlineData(100, 10, "Some more text")]
        public void TestScrollMessage(int type, int tip, string text)
        {
            var expected = new ScrollMessage(type, tip, text).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendScrollMessage(type, tip ,text);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Theory]
        [InlineData(14, 10, 05)]
        public void TestCurrentTime(int hour, int minute, int second)
        {
            var date = new DateTime(2020, 1, 1, hour, minute, second);
            var expected = new CurrentTime(date).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendCurrentTime(date);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Theory]
        [InlineData(1000, 10, 1)]
        public void TestPathfindMessage(int x, int y, int z)
        {
            var p = new Point3D(x, y, z);
            var expected = new PathfindMessage(p).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendPathfindMessage(p);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(100)]
        public void TestPingAck(byte ping)
        {
            var expected = new PingAck(ping).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendPingAck(ping);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Theory]
        [InlineData(0xFF01)]
        public void TestDisplayHuePicker(int itemID)
        {
            var huePicker = new HuePicker(itemID);

            var expected = new DisplayHuePicker(huePicker).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendDisplayHuePicker(huePicker.Serial, huePicker.ItemID);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }
    }
}
