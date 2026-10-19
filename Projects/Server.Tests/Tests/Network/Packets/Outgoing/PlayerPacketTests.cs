using System;
using Server.HuePickers;
using Server.Network;
using Xunit;

namespace Server.Tests.Network;

[Collection("Sequential Server Tests")]
public class PlayerPacketTests
{
    [Fact]
    public void TestStatLockInfo()
    {
        var m = new Mobile((Serial)0x1);
        m.DefaultMobileInit();
        m.StrLock = StatLockType.Down;
        m.IntLock = StatLockType.Up;
        m.DexLock = StatLockType.Locked;

        PacketAssert.Sends(new StatLockInfo(m), ns => ns.SendStatLockInfo(m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(200)]
    public void TestChangeUpdateRange(int range)
    {
        PacketAssert.Sends(new ChangeUpdateRange(range), ns => ns.SendChangeUpdateRange((byte)range));
    }

    [Fact]
    public void TestDeathStatus()
    {
        PacketAssert.Sends(new DeathStatus(false), ns => ns.SendDeathStatus());
    }

    [Theory]
    [InlineData(0x1000u, "This is a header", "This is a body", "This is a footer")]
    [InlineData(0x1000u, null, null, null)]
    public void TestDisplayProfile(uint serial, string header, string body, string footer)
    {
        PacketAssert.Sends(
            new DisplayProfile((Serial)serial, header, body, footer),
            ns => ns.SendDisplayProfile((Serial)serial, header, body, footer)
        );
    }

    [Theory]
    [InlineData(LRReason.CannotLift)]
    [InlineData(LRReason.TryToSteal)]
    public void TestLiftRej(LRReason reason)
    {
        PacketAssert.Sends(new LiftRej(reason), ns => ns.SendLiftReject(reason));
    }

    [Fact]
    public void TestLogoutAck()
    {
        PacketAssert.Sends(new LogoutAck(), ns => ns.SendLogoutAck());
    }

    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(4, 5, 6)]
    [InlineData(0xFF, 0x46, 0xEC)]
    public void TestWeather(byte type, byte density, byte temp)
    {
        PacketAssert.Sends(new Weather(type, density, temp), ns => ns.SendWeather(type, density, temp));
    }

    [Fact]
    public void TestServerChange()
    {
        var p = new Point3D(100, 1000, 1);
        var map = Map.Maps[0];

        PacketAssert.Sends(new ServerChange(p, map), ns => ns.SendServerChange(p, map));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(255)]
    public void TestSequence(byte num)
    {
        PacketAssert.Sends(new Sequence(num), ns => ns.SendSequence(num));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("This is a URL, I promise")]
    [InlineData("https://www.modernuo.com")]
    public void TestLaunchBrowser(string uri)
    {
        PacketAssert.Sends(new LaunchBrowser(uri), ns => ns.SendLaunchBrowser(uri));
    }

    [Fact]
    public void TestDragEffect()
    {
        var src = new Entity((Serial)0x1000, new Point3D(1000, 100, 10), null);
        var targ = new Entity((Serial)0x2000, new Point3D(1125, 125, 5), null);
        const int itemId = 0x384;
        const int hue = 1024;
        const int amount = 25;

        PacketAssert.Sends(
            new DragEffect(src, targ, itemId, hue, amount),
            ns => ns.SendDragEffect(src.Serial, src.Location, targ.Serial, targ.Location, itemId, hue, amount)
        );
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public void TestSeasonChange(int season, bool playSound)
    {
        PacketAssert.Sends(new SeasonChange(season, playSound), ns => ns.SendSeasonChange((byte)season, playSound));
    }

    [Theory]
    [InlineData(0x1024u, "Test Title", true, true)]
    [InlineData(0x1024u, "Test Title", false, true)]
    [InlineData(0x1024u, "Test Title", true, false)]
    public void TestDisplayPaperdoll(uint m, string title, bool warmode, bool canLift)
    {
        PacketAssert.Sends(
            new DisplayPaperdoll((Serial)m, title, warmode, canLift),
            ns => ns.SendDisplayPaperdoll((Serial)m, title, warmode, canLift)
        );
    }

    [Theory]
    [InlineData(MusicName.Approach)]
    [InlineData(MusicName.Combat1)]
    [InlineData(MusicName.ValoriaShips)]
    public void TestPlayMusic(MusicName music)
    {
        PacketAssert.Sends(new PlayMusic(music), ns => ns.SendPlayMusic(music));
    }

    [Theory]
    [InlineData(10, 1, "Some text")]
    [InlineData(100, 10, "Some more text")]
    public void TestScrollMessage(int type, int tip, string text)
    {
        PacketAssert.Sends(new ScrollMessage(type, tip, text), ns => ns.SendScrollMessage(type, tip, text));
    }

    [Fact]
    public void TestCurrentTime()
    {
        var date = new DateTime(2020, 1, 1, 14, 10, 5);

        PacketAssert.Sends(new CurrentTime(date), ns => ns.SendCurrentTime(date));
    }

    [Fact]
    public void TestPathfindMessage()
    {
        var p = new Point3D(1000, 10, 1);

        PacketAssert.Sends(new PathfindMessage(p), ns => ns.SendPathfindMessage(p));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(100)]
    public void TestPingAck(byte ping)
    {
        PacketAssert.Sends(new PingAck(ping), ns => ns.SendPingAck(ping));
    }

    [Fact]
    public void TestDisplayHuePicker()
    {
        var huePicker = new HuePicker(0xFF01);

        PacketAssert.Sends(
            new DisplayHuePicker(huePicker),
            ns => ns.SendDisplayHuePicker(huePicker.Serial, huePicker.ItemID)
        );
    }
}
