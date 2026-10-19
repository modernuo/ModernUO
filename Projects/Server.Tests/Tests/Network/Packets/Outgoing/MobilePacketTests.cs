using Server.Network;
using Xunit;

namespace Server.Tests.Network;

[Collection("Sequential Server Tests")]
public class MobilePacketTests
{
    private static Mobile CreateMobileWithStats(uint serial = 0x1, string name = null)
    {
        var m = new Mobile((Serial)serial) { Name = name };
        m.DefaultMobileInit();
        m.Str = 50;
        m.Hits = 100;
        m.Int = 75;
        m.Mana = 100;
        m.Dex = 25;
        m.Stam = 100;

        return m;
    }

    private sealed class TestPoison : Poison
    {
        public TestPoison(int level) : base(level) => Level = level;

        public override string Name => $"Test Poison {Level}";
        public override int Level { get; }
        public override PoisonFamily Family => PoisonFamily.Standard;

        public override Timer ConstructTimer(Mobile m) => null;
    }

    [Fact]
    public void TestDeathAnimation()
    {
        var killed = (Serial)0x1;
        var corpse = (Serial)0x1000;

        PacketAssert.Sends(new DeathAnimation(killed, corpse), ns => ns.SendDeathAnimation(killed, corpse));
    }

    [Fact]
    public void TestBondStatus()
    {
        var petSerial = (Serial)0x1;
        const bool bonded = true;

        PacketAssert.Sends(new BondedStatus(petSerial, bonded), ns => ns.SendBondedStatus(petSerial, bonded));
    }

    [Theory]
    [InlineData(ProtocolChanges.StygianAbyss)]
    [InlineData(ProtocolChanges.None)]
    public void TestMobileMoving(ProtocolChanges protocolChanges)
    {
        var m = new Mobile((Serial)0x1);
        m.DefaultMobileInit();

        var noto = 10;

        PacketAssert.Sends(
            ns => new MobileMoving(m, noto, ns.StygianAbyss),
            ns => ns.SendMobileMoving(m, noto),
            protocolChanges
        );
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Kamron")]
    [InlineData("Some Really Long Mobile Name That Gets Cut off")]
    public void TestMobileName(string name)
    {
        var m = new Mobile((Serial)0x1) { Name = name };
        m.DefaultMobileInit();

        PacketAssert.Sends(new MobileName(m), ns => ns.SendMobileName(m));
    }

    [Theory]
    [InlineData(200, 5, 1, false, false, 5)]
    [InlineData(10, 100, 25, true, false, 0)]
    public void TestMobileAnimation(int action, int frameCount, int repeatCount, bool reverse, bool repeat, byte delay)
    {
        var mobile = (Serial)0x1;

        PacketAssert.Sends(
            new MobileAnimation(mobile, action, frameCount, repeatCount, !reverse, repeat, delay),
            ns => ns.SendMobileAnimation(mobile, action, frameCount, repeatCount, !reverse, repeat, delay)
        );
    }

    [Theory]
    [InlineData(200, 5, 5)]
    [InlineData(10, 100, 20)]
    public void TestNewMobileAnimation(int action, int frameCount, byte delay)
    {
        var mobile = (Serial)0x1;

        PacketAssert.Sends(
            new NewMobileAnimation(mobile, action, frameCount, delay),
            ns => ns.SendNewMobileAnimation(mobile, action, frameCount, delay)
        );
    }

    // Level -1 means not poisoned. Constructed directly: the poison registry is filled by UOContent.
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(4)]
    public void TestHealthbarPoison(int level)
    {
        var m = new Mobile((Serial)0x1);
        m.DefaultMobileInit();
        m.Poison = level < 0 ? null : new TestPoison(level);

        PacketAssert.Sends(new HealthbarPoison(m), ns => ns.SendMobileHealthbar(m, Healthbar.Poison));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void TestYellowBar(bool isBlessed, bool isYellowHealth)
    {
        var m = new Mobile((Serial)0x1);
        m.DefaultMobileInit();
        m.Blessed = isBlessed;
        m.YellowHealthbar = isYellowHealth;

        PacketAssert.Sends(new HealthbarYellow(m), ns => ns.SendMobileHealthbar(m, Healthbar.Yellow));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TestMobileStatusCompact(bool canBeRenamed)
    {
        var m = CreateMobileWithStats(name: "Random Mobile 1");

        PacketAssert.Sends(new MobileStatusCompact(canBeRenamed, m), ns => ns.SendMobileStatusCompact(m, canBeRenamed));
    }

    [Theory]
    [InlineData(ProtocolChanges.Version70610)]
    [InlineData(ProtocolChanges.Version502b)]
    public void TestMobileStatus(ProtocolChanges changes)
    {
        var beholder = CreateMobileWithStats(0x1, "Random Mobile 1");
        var beheld = CreateMobileWithStats(0x2, "Random Mobile 2");

        PacketAssert.Sends(
            ns => new MobileStatus(beholder, beheld, ns),
            ns => ns.SendMobileStatus(beholder, beheld),
            changes
        );
    }

    [Theory]
    [InlineData(ProtocolChanges.Version70610, "7.0.61.0", ClientFlags.TerMur, Expansion.HS, 6)]
    [InlineData(ProtocolChanges.Version502b, "5.0.2b", ClientFlags.Malas, Expansion.ML, 5)]
    public void TestMobileStatusExtendedSelf(
        ProtocolChanges changes,
        string version,
        ClientFlags clientFlags,
        Expansion expansion,
        int mobileStatusVersion
    )
    {
        var oldExpansion = Core.Expansion;
        var expansionInfo = ExpansionInfo.GetInfo(expansion);
        var oldVersion = expansionInfo.MobileStatusVersion;

        try
        {
            Core.Expansion = expansion;
            expansionInfo.MobileStatusVersion = mobileStatusVersion;

            var m = CreateMobileWithStats(name: "Random Mobile 1");

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.ProtocolChanges = changes;
            ns.Version = new ClientVersion(version);
            ns.Flags = clientFlags;

            var expected = new MobileStatusExtended(m, ns).Compile();
            ns.SendMobileStatus(m, m);

            AssertThat.Equal(ns.SendBuffer.GetReadSpan(), expected);
        }
        finally
        {
            Core.Expansion = oldExpansion;
            expansionInfo.MobileStatusVersion = oldVersion;
        }
    }

    [Theory]
    [InlineData(ProtocolChanges.None, 0)]
    [InlineData(ProtocolChanges.StygianAbyss, 100)]
    public void TestMobileUpdate(ProtocolChanges changes, int solidHueOverride)
    {
        var m = new Mobile((Serial)0x1);
        m.DefaultMobileInit();
        m.SolidHueOverride = solidHueOverride;

        PacketAssert.Sends(ns => new MobileUpdate(m, ns.StygianAbyss), ns => ns.SendMobileUpdate(m), changes);
    }

    [Theory]
    [InlineData(ProtocolChanges.Version70331, 0, 0, 0, 0)]
    [InlineData(ProtocolChanges.Version70331, 10, 1024, 0, 0)]
    [InlineData(ProtocolChanges.Version70331, 10, 1024, 11, 2048)]
    [InlineData(ProtocolChanges.Version6000, 0, 0, 0, 0)]
    [InlineData(ProtocolChanges.Version6000, 10, 1024, 0, 0)]
    [InlineData(ProtocolChanges.Version6000, 10, 1024, 11, 2048)]
    [InlineData(ProtocolChanges.Version7000, 0, 0, 0, 0)]
    [InlineData(ProtocolChanges.Version7000, 10, 1024, 0, 0)]
    [InlineData(ProtocolChanges.Version7000, 10, 1024, 11, 2048)]
    public void TestMobileIncoming(
        ProtocolChanges changes, int hairItemId, int hairHue, int facialHairItemId, int facialHairHue
    )
    {
        var beholder = new Mobile((Serial)0x1)
        {
            Name = "Random Mobile 1"
        };
        beholder.DefaultMobileInit();

        var beheld = new Mobile((Serial)0x2)
        {
            Name = "Random Mobile 2"
        };
        beheld.DefaultMobileInit();
        beheld.AddItem(
            new Item((Serial)0x1000)
            {
                Layer = Layer.OneHanded
            }
        );

        // Test Dupe
        beheld.AddItem(
            new Item((Serial)0x1001)
            {
                Layer = Layer.OneHanded
            }
        );

        beheld.HairItemID = hairItemId;
        beheld.HairHue = hairHue;
        beheld.FacialHairItemID = facialHairItemId;
        beheld.FacialHairHue = facialHairHue;

        PacketAssert.Sends(
            ns => new MobileIncoming(ns, beholder, beheld),
            ns => ns.SendMobileIncoming(beholder, beheld),
            changes
        );
    }

    [Fact]
    public void TestMobileHits()
    {
        var m = CreateMobileWithStats();

        PacketAssert.Sends(new MobileHits(m), ns => ns.SendMobileHits(m));
    }

    [Fact]
    public void TestMobileHitsN()
    {
        var m = CreateMobileWithStats();

        PacketAssert.Sends(new MobileHitsN(m), ns => ns.SendMobileHits(m, true));
    }

    [Fact]
    public void TestMobileMana()
    {
        var m = CreateMobileWithStats();

        PacketAssert.Sends(new MobileMana(m), ns => ns.SendMobileMana(m));
    }

    [Fact]
    public void TestMobileManaN()
    {
        var m = CreateMobileWithStats();

        PacketAssert.Sends(new MobileManaN(m), ns => ns.SendMobileMana(m, true));
    }

    [Fact]
    public void TestMobileStam()
    {
        var m = CreateMobileWithStats();

        PacketAssert.Sends(new MobileStam(m), ns => ns.SendMobileStam(m));
    }

    [Fact]
    public void TestMobileStamN()
    {
        var m = CreateMobileWithStats();

        PacketAssert.Sends(new MobileStamN(m), ns => ns.SendMobileStam(m, true));
    }

    [Fact]
    public void TestMobileAttributes()
    {
        var m = CreateMobileWithStats();

        PacketAssert.Sends(new MobileAttributes(m), ns => ns.SendMobileAttributes(m));
    }

    [Fact]
    public void TestMobileAttributesN()
    {
        var m = CreateMobileWithStats();

        PacketAssert.Sends(new MobileAttributesN(m), ns => ns.SendMobileAttributes(m, true));
    }

    [Fact]
    public void TestRemoveEntity()
    {
        var e = (Serial)0x1000;

        PacketAssert.Sends(new RemoveEntity(e), ns => ns.SendRemoveEntity(e));
    }
}
