using System;
using Server.Network;
using Xunit;

namespace Server.Tests.Network
{
    [Collection("Sequential Tests")]
    public class MobilePacketTests : IClassFixture<ServerFixture>
    {
        [Fact]
        public void TestDeathAnimation()
        {
            Serial killed = (Serial)0x1;
            Serial corpse = (Serial)0x1000;

            var expected = new DeathAnimation(killed, corpse).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendDeathAnimation(killed, corpse);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestBondStatus()
        {
            Serial petSerial = (Serial)0x1;
            const bool bonded = true;

            var expected = new BondedStatus(petSerial, bonded).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendBondedStatus(petSerial, bonded);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Theory]
        [InlineData(ProtocolChanges.StygianAbyss)]
        [InlineData(ProtocolChanges.None)]
        public void TestMobileMoving(ProtocolChanges protocolChanges)
        {
            var m = new Mobile((Serial)0x1);
            m.DefaultMobileInit();

            var noto = 10;

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.ProtocolChanges = protocolChanges;
            var expected = new MobileMoving(m, noto, ns.StygianAbyss).Compile();

            ns.SendMobileMoving(m, noto);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("Kamron")]
        [InlineData("Some Really Long Mobile Name That Gets Cut off")]
        public void TestMobileName(string name)
        {
            var m = new Mobile((Serial)0x1) { Name = name };
            m.DefaultMobileInit();

            var expected = new MobileName(m).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMobileName(m);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Theory]
        [InlineData(200, 5, 1, false, false, 5)]
        [InlineData(10, 100, 25, true, false, 0)]
        public void TestMobileAnimation(int action, int frameCount, int repeatCount, bool reverse, bool repeat, byte delay)
        {
            Serial mobile = (Serial)0x1;

            var expected = new MobileAnimation(
                mobile,
                action,
                frameCount,
                repeatCount,
                !reverse,
                repeat,
                delay
            ).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMobileAnimation(
                mobile,
                action,
                frameCount,
                repeatCount,
                !reverse,
                repeat,
                delay
            );

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Theory]
        [InlineData(200, 5, 5)]
        [InlineData(10, 100, 20)]
        public void TestNewMobileAnimation(int action, int frameCount, byte delay)
        {
            Serial mobile = (Serial)0x1;

            var expected = new NewMobileAnimation(
                mobile,
                action,
                frameCount,
                delay
            ).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendNewMobileAnimation(
                mobile,
                action,
                frameCount,
                delay
            );

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Theory]
        [InlineData("None")]
        [InlineData("Lesser")]
        [InlineData("Lethal")]
        public void TestHealthbarPoison(string pName)
        {
            var p = Poison.GetPoison(pName);
            var m = new Mobile((Serial)0x1);
            m.DefaultMobileInit();
            m.Poison = p;

            var expected = new HealthbarPoison(m).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMobileHealthbar(m, Healthbar.Poison);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
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

            var expected = new HealthbarYellow(m).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMobileHealthbar(m, Healthbar.Yellow);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestMobileStatusCompact(bool canBeRenamed)
        {
            var m = new Mobile((Serial)0x1) { Name = "Random Mobile 1" };
            m.DefaultMobileInit();
            m.Str = 50;
            m.Hits = 100;
            m.Int = 75;
            m.Mana = 100;
            m.Dex = 25;
            m.Stam = 100;

            var expected = new MobileStatusCompact(canBeRenamed, m).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMobileStatusCompact(m, canBeRenamed);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Theory]
        [InlineData(ProtocolChanges.Version70610)]
        [InlineData(ProtocolChanges.Version400a)]
        [InlineData(ProtocolChanges.Version502b)]
        public void TestMobileStatus(ProtocolChanges changes)
        {
            var beholder = new Mobile((Serial)0x1) { Name = "Random Mobile 1" };
            beholder.DefaultMobileInit();
            beholder.Str = 50;
            beholder.Hits = 100;
            beholder.Int = 75;
            beholder.Mana = 100;
            beholder.Dex = 25;
            beholder.Stam = 100;

            var beheld = new Mobile((Serial)0x2) { Name = "Random Mobile 2" };
            beheld.DefaultMobileInit();
            beheld.Str = 50;
            beheld.Hits = 100;
            beheld.Int = 75;
            beheld.Mana = 100;
            beheld.Dex = 25;
            beheld.Stam = 100;

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.ProtocolChanges = changes;

            var expected = new MobileStatus(beholder, beheld, ns).Compile();
            ns.SendMobileStatus(beholder, beheld);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Theory]
        [InlineData(ProtocolChanges.Version70610, "7.0.61.0", ClientFlags.TerMur, Expansion.HS, 6)]
        [InlineData(ProtocolChanges.Version400a, "4.0.0a", ClientFlags.Malas, Expansion.AOS, 4)]
        [InlineData(ProtocolChanges.Version502b, "5.0.2b", ClientFlags.Malas, Expansion.ML, 5)]
        public void TestMobileStatusExtendedSelf(
            ProtocolChanges changes,
            string version,
            ClientFlags clientFlags,
            Expansion expansion,
            int mobileStatusVersion
        )
        {
            var expansionInfo = ExpansionInfo.GetInfo(Core.Expansion);
            var oldExpansion = Core.Expansion;
            var oldVersion = expansionInfo.MobileStatusVersion;
            Core.Expansion = expansion;
            ExpansionInfo.GetInfo(Core.Expansion).MobileStatusVersion = mobileStatusVersion;

            var m = new Mobile((Serial)0x1) { Name = "Random Mobile 1" };
            m.DefaultMobileInit();
            m.Str = 50;
            m.Hits = 100;
            m.Int = 75;
            m.Mana = 100;
            m.Dex = 25;
            m.Stam = 100;

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.ProtocolChanges = changes;
            ns.Version = new ClientVersion(version);
            ns.Flags = clientFlags;

            var expected = new MobileStatusExtended(m, ns).Compile();
            ns.SendMobileStatus(m, m);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
            Core.Expansion = oldExpansion;
            expansionInfo.MobileStatusVersion = oldVersion;
        }

        [Theory]
        [InlineData(ProtocolChanges.None, 0)]
        [InlineData(ProtocolChanges.StygianAbyss, 100)]
        public void TestMobileUpdate(ProtocolChanges changes, int solidHueOverride)
        {
            var m = new Mobile((Serial)0x1);
            m.DefaultMobileInit();
            m.SolidHueOverride = solidHueOverride;

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.ProtocolChanges = changes;

            var expected = new MobileUpdate(m, ns.StygianAbyss).Compile();
            ns.SendMobileUpdate(m);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
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

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.ProtocolChanges = changes;

            var expected = new MobileIncoming(ns, beholder, beheld).Compile();
            ns.SendMobileIncoming(beholder, beheld);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestMobileHits()
        {
            var m = new Mobile((Serial)0x1);
            m.DefaultMobileInit();
            m.Str = 100;
            m.Hits = 100;

            var expected = new MobileHits(m).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMobileHits(m);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestMobileHitsN()
        {
            var m = new Mobile((Serial)0x1);
            m.DefaultMobileInit();
            m.Str = 100;
            m.Hits = 100;

            var expected = new MobileHitsN(m).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMobileHits(m, true);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestMobileMana()
        {
            var m = new Mobile((Serial)0x1);
            m.DefaultMobileInit();
            m.Int = 75;
            m.Mana = 100;

            var expected = new MobileMana(m).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMobileMana(m);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestMobileManaN()
        {
            var m = new Mobile((Serial)0x1);
            m.DefaultMobileInit();
            m.Int = 75;
            m.Mana = 100;

            var expected = new MobileManaN(m).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMobileMana(m, true);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestMobileStam()
        {
            var m = new Mobile((Serial)0x1);
            m.DefaultMobileInit();
            m.Dex = 75;
            m.Stam = 100;

            var expected = new MobileStam(m).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMobileStam(m);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestMobileStamN()
        {
            var m = new Mobile((Serial)0x1);
            m.DefaultMobileInit();
            m.Dex = 75;
            m.Stam = 100;

            var expected = new MobileStamN(m).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMobileStam(m, true);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestMobileAttributes()
        {
            var m = new Mobile((Serial)0x1);
            m.DefaultMobileInit();
            m.Str = 50;
            m.Hits = 100;
            m.Int = 75;
            m.Mana = 100;
            m.Dex = 25;
            m.Stam = 100;

            var expected = new MobileAttributes(m).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMobileAttributes(m);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestMobileAttributesN()
        {
            var m = new Mobile((Serial)0x1);
            m.DefaultMobileInit();
            m.Str = 50;
            m.Hits = 100;
            m.Int = 75;
            m.Mana = 100;
            m.Dex = 25;
            m.Stam = 100;

            var expected = new MobileAttributesN(m).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMobileAttributes(m, true);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestRemoveEntity()
        {
            Serial e = (Serial)0x1000;
            var expected = new RemoveEntity(e).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendRemoveEntity(e);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }
    }
}
