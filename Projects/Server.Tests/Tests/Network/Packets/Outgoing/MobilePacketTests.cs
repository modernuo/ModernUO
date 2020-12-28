using System;
using System.Buffers;
using Server.Network;
using Xunit;

namespace Server.Tests.Network
{
    public class MobilePacketTests : IClassFixture<ServerFixture>
    {
        [Fact]
        public void TestDeathAnimation()
        {
            Serial killed = 0x1;
            Serial corpse = 0x1000;

            var expected = new DeathAnimation(killed, corpse).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendDeathAnimation(killed, corpse);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestBondStatus()
        {
            Serial petSerial = 0x1;
            const bool bonded = true;

            var expected = new BondedStatus(petSerial, bonded).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendBondedStatus(petSerial, bonded);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Theory]
        [InlineData(ProtocolChanges.StygianAbyss)]
        [InlineData(ProtocolChanges.None)]
        public void TestMobileMoving(ProtocolChanges protocolChanges)
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            var noto = 10;

            using var ns = PacketTestUtilities.CreateTestNetState();
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
            var m = new Mobile(0x1) { Name = name };
            m.DefaultMobileInit();

            var expected = new MobileName(m).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMobileName(m);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Theory]
        [InlineData(200, 5, 1, false, false, 5)]
        [InlineData(10, 100, 25, true, false, 0)]
        public void TestMobileAnimation(int action, int frameCount, int repeatCount, bool reverse, bool repeat, byte delay)
        {
            Serial mobile = 0x1;

            var expected = new MobileAnimation(
                mobile,
                action,
                frameCount,
                repeatCount,
                !reverse,
                repeat,
                delay
            ).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
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
            Serial mobile = 0x1;

            var expected = new NewMobileAnimation(
                mobile,
                action,
                frameCount,
                delay
            ).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
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
            var m = new Mobile(0x1);
            m.DefaultMobileInit();
            m.Poison = p;

            var expected = new HealthbarPoison(m).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
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
            var m = new Mobile(0x1);
            m.DefaultMobileInit();
            m.Blessed = isBlessed;
            m.YellowHealthbar = isYellowHealth;

            var expected = new HealthbarYellow(m).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMobileHealthbar(m, Healthbar.Yellow);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestMobileStatusCompact(bool canBeRenamed)
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            var expected = new MobileStatusCompact(canBeRenamed, m).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMobileStatusCompact(m, canBeRenamed);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Theory]
        [InlineData(ProtocolChanges.Version70610)]
        [InlineData(ProtocolChanges.Version400a)]
        [InlineData(ProtocolChanges.Version502b)]
        public void TestMobileStatusExtended(ProtocolChanges changes)
        {
            var beholder = new Mobile(0x1)
            {
                Name = "Random Mobile 1"
            };
            beholder.DefaultMobileInit();

            var beheld = new Mobile(0x2)
            {
                Name = "Random Mobile 2"
            };
            beheld.DefaultMobileInit();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.ProtocolChanges = changes;

            var expected = new MobileStatus(beholder, beheld, ns).Compile();
            ns.SendMobileStatus(beholder, beheld);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestMobileUpdate()
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            var data = new MobileUpdate(m, true).Compile();

            Span<byte> expectedData = stackalloc byte[19];
            var pos = 0;

            var hue = m.SolidHueOverride >= 0 ? m.SolidHueOverride : m.Hue;

            expectedData.Write(ref pos, (byte)0x20); // Packet ID
            expectedData.Write(ref pos, m.Serial);
            expectedData.Write(ref pos, (ushort)m.Body);
#if NO_LOCAL_INIT
             expectedData.Write(ref pos, (byte)0); // Unknown
#else
            pos++;
#endif
            expectedData.Write(ref pos, (ushort)hue);
            expectedData.Write(ref pos, (byte)m.GetPacketFlags(true));
            expectedData.Write(ref pos, (ushort)m.X);
            expectedData.Write(ref pos, (ushort)m.Y);
#if NO_LOCAL_INIT
            expectedData.Write(ref pos, (ushort)2); // Unknown
#else
            pos += 2;
#endif
            expectedData.Write(ref pos, (byte)m.Direction);
            expectedData.Write(ref pos, (byte)m.Z);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestMobileUpdateOld()
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            var data = new MobileUpdate(m, false).Compile();

            Span<byte> expectedData = stackalloc byte[19];
            var pos = 0;

            var hue = m.SolidHueOverride >= 0 ? m.SolidHueOverride : m.Hue;

            expectedData.Write(ref pos, (byte)0x20); // Packet ID
            expectedData.Write(ref pos, m.Serial);
            expectedData.Write(ref pos, (ushort)m.Body);
#if NO_LOCAL_INIT
            expectedData.Write(ref pos, (byte)0); // Unknown
#else
            pos++;
#endif
            expectedData.Write(ref pos, (ushort)hue);
            expectedData.Write(ref pos, (byte)m.GetPacketFlags(false));
            expectedData.Write(ref pos, (ushort)m.X);
            expectedData.Write(ref pos, (ushort)m.Y);
#if NO_LOCAL_INIT
            expectedData.Write(ref pos, (ushort)2); // Unknown
#else
            pos += 2;
#endif
            expectedData.Write(ref pos, (byte)m.Direction);
            expectedData.Write(ref pos, (byte)m.Z);

            AssertThat.Equal(data, expectedData);
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
            ProtocolChanges protocolChanges, int hairItemId, int hairHue, int facialHairItemId, int facialHairHue
        )
        {
            var beholder = new Mobile(0x1)
            {
                Name = "Random Mobile 1"
            };
            beholder.DefaultMobileInit();

            var beheld = new Mobile(0x2)
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

            var ns = new NetState(null)
            {
                ProtocolChanges = protocolChanges
            };

            var data = new MobileIncoming(ns, beholder, beheld).Compile();

            var sa = ns.StygianAbyss;
            var newPacket = ns.NewMobileIncoming;
            var itemIdMask = newPacket ? 0xFFFF : 0x7FFF;

            Span<bool> layers = stackalloc bool[256];
#if NO_LOCAL_INIT
            layers.Clear();
#endif

            var items = beheld.Items;
            var count = items.Count;

            if (beheld.HairItemID > 0)
            {
                count++;
            }

            if (beheld.FacialHairItemID > 0)
            {
                count++;
            }

            var length = 23 + count * 9; // Max Size

            Span<byte> expectedData = stackalloc byte[length];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x78);
            pos += 2; // Length

            var isSolidHue = beheld.SolidHueOverride >= 0;

            expectedData.Write(ref pos, beheld.Serial);
            expectedData.Write(ref pos, (ushort)beheld.Body);
            expectedData.Write(ref pos, (ushort)beheld.X);
            expectedData.Write(ref pos, (ushort)beheld.Y);
            expectedData.Write(ref pos, (byte)beheld.Z);
            expectedData.Write(ref pos, (byte)beheld.Direction);
            expectedData.Write(ref pos, (ushort)(isSolidHue ? beheld.SolidHueOverride : beheld.Hue));
            expectedData.Write(ref pos, (byte)beheld.GetPacketFlags(sa));
            expectedData.Write(ref pos, (byte)Notoriety.Compute(beholder, beheld));

            byte layer;

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];

                layer = (byte)item.Layer;

                if (!item.Deleted && !layers[layer] && beholder.CanSee(item))
                {
                    layers[layer] = true;

                    expectedData.Write(ref pos, item.Serial);

                    var hue = isSolidHue ? beheld.SolidHueOverride : item.Hue;
                    var itemID = item.ItemID & itemIdMask;
                    var writeHue = newPacket || hue != 0;

                    if (!newPacket)
                    {
                        itemID |= 0x8000;
                    }

                    expectedData.Write(ref pos, (ushort)itemID);
                    expectedData.Write(ref pos, layer);
                    if (writeHue)
                    {
                        expectedData.Write(ref pos, (ushort)hue);
                    }
                }
            }

            layer = (byte)Layer.Hair;
            var itemId = beheld.HairItemID;

            if (itemId > 0 && !layers[layer])
            {
                expectedData.Write(ref pos, HairInfo.FakeSerial(beheld));
                var hue = isSolidHue ? beheld.SolidHueOverride : beheld.HairHue;
                itemId &= itemIdMask;
                var writeHue = newPacket || hue != 0;

                if (!newPacket)
                {
                    itemId |= 0x8000;
                }

                expectedData.Write(ref pos, (ushort)itemId);
                expectedData.Write(ref pos, layer);
                if (writeHue)
                {
                    expectedData.Write(ref pos, (ushort)hue);
                }
            }

            layer = (byte)Layer.FacialHair;
            itemId = beheld.FacialHairItemID;

            if (itemId > 0 && !layers[layer])
            {
                expectedData.Write(ref pos, FacialHairInfo.FakeSerial(beheld));
                var hue = isSolidHue ? beheld.SolidHueOverride : beheld.FacialHairHue;
                itemId &= itemIdMask;
                var writeHue = newPacket || hue != 0;

                if (!newPacket)
                {
                    itemId |= 0x8000;
                }

                expectedData.Write(ref pos, (ushort)itemId);
                expectedData.Write(ref pos, layer);
                if (writeHue)
                {
                    expectedData.Write(ref pos, (ushort)hue);
                }
            }

#if NO_LOCAL_INIT
            expectedData.Write(ref pos, 0); // Zero serial, terminate list
#else
            pos += 4;
#endif

            expectedData.Slice(1, 2).Write((ushort)pos); // Length
            expectedData = expectedData.Slice(0, pos);

            AssertThat.Equal(data, expectedData);
        }
    }

    [Collection("Sequential Tests")]
    public class SequentialMobilePacketTests : IClassFixture<ServerFixture>
    {
        [Fact]
        public void TestMobileHits()
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();
            m.Str = 100;
            m.Hits = 100;

            var expected = new MobileHits(m).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMobileHits(m);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestMobileHitsN()
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();
            m.Str = 100;
            m.Hits = 100;

            var expected = new MobileHitsN(m).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMobileHits(m, true);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestMobileMana()
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();
            m.Int = 75;
            m.Mana = 100;

            var expected = new MobileMana(m).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMobileMana(m);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestMobileManaN()
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();
            m.Int = 75;
            m.Mana = 100;

            var expected = new MobileManaN(m).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMobileMana(m, true);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestMobileStam()
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();
            m.Dex = 75;
            m.Stam = 100;

            var expected = new MobileStam(m).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMobileStam(m);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestMobileStamN()
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();
            m.Dex = 75;
            m.Stam = 100;

            var expected = new MobileStamN(m).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMobileStam(m, true);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestMobileAttributes()
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();
            m.Str = 50;
            m.Hits = 100;
            m.Int = 75;
            m.Mana = 100;
            m.Dex = 25;
            m.Stam = 100;

            var expected = new MobileAttributes(m).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMobileAttributes(m);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestMobileAttributesN()
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();
            m.Str = 50;
            m.Hits = 100;
            m.Int = 75;
            m.Mana = 100;
            m.Dex = 25;
            m.Stam = 100;

            var expected = new MobileAttributesN(m).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMobileAttributes(m, true);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }
    }
}
