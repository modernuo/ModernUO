using System.Linq;
using Server;
using Server.Multis;
using Server.Network;
using Server.Tests;
using Server.Tests.Network;
using Xunit;

namespace UOContent.Tests
{
    public class HousePacketTests
    {
        [Theory]
        [InlineData(0x1001u)]
        public void TestBeginHouseCustomization(uint serial)
        {
            var expected = new BeginHouseCustomization((Serial)serial).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendBeginHouseCustomization((Serial)serial);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
        }

        [Theory]
        [InlineData(0x1001u)]
        public void TestEndHouseCustomization(uint serial)
        {
            var expected = new EndHouseCustomization((Serial)serial).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendEndHouseCustomization((Serial)serial);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
        }

        [Theory]
        [InlineData(0x1001u, 0)]
        [InlineData(0x1001u, 100)]
        public void TestDesignStateGeneral(uint serial, int revision)
        {
            var expected = new DesignStateGeneral((Serial)serial, revision).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendDesignStateGeneral((Serial)serial, revision);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
        }

        [Fact]
        public void TestHouseDesignStateDetailed()
        {
            Serial serial = (Serial)0x40000001;
            var revision = 10;
            var tiles = new MultiTileEntry[250];
            for (var i = 0; i < tiles.Length; i++)
            {
                tiles[i] = new MultiTileEntry(
                    (ushort)i,
                    (byte)i,
                    (byte)i,
                    (byte)(i / 50),
                    TileFlag.None
                );
            }
            var mcl = new MultiComponentList(tiles.ToList());

            var expected = new DesignStateDetailed(
                serial, revision, mcl.Min.X, mcl.Min.Y, mcl.Max.X, mcl.Max.Y, tiles
            ).Compile();

            var actual = HousePackets.CreateHouseDesignStateDetailed(serial, revision, mcl);

            AssertThat.Equal(actual, expected);
        }
    }
}
