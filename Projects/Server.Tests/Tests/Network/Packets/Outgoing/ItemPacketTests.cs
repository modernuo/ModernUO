using Server.Network;
using Xunit;

namespace Server.Tests.Network;

[Collection("Sequential Server Tests")]
public class ItemPacketTests
{
    [Fact]
    public void TestWorldItemPacket()
    {
        var serial = (Serial)0x1024;
        var itemId = 1;

        // Move to fixture
        TileData.ItemTable[itemId] = new ItemData(
            "Test Item Data",
            TileFlag.Generic,
            1,
            1,
            1,
            1,
            1,
            1
        );

        var item = new Item(serial)
        {
            ItemID = itemId,
            Hue = 0x1024,
            Amount = 10,
            Location = new Point3D(1000, 100, -10),
            Direction = Direction.Left
        };

        var expected = new WorldItem(item).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendWorldItem(item);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestWorldItemSAPacket()
    {
        var serial = (Serial)0x1024;
        ushort itemId = 1;

        // Move to fixture
        TileData.ItemTable[itemId] = new ItemData(
            "Test Item Data",
            TileFlag.Generic,
            1,
            1,
            1,
            1,
            1,
            1
        );

        var item = new Item(serial)
        {
            ItemID = itemId,
            Hue = 0x1024,
            Amount = 10,
            Location = new Point3D(1000, 100, -10)
        };

        var expected = new WorldItemSA(item).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.ProtocolChanges = ProtocolChanges.StygianAbyss;
        ns.SendWorldItem(item);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestWorldItemHSPacket()
    {
        var serial = (Serial)0x1024;
        var itemId = 1;

        // Move to fixture
        TileData.ItemTable[itemId] = new ItemData(
            "Test Item Data",
            TileFlag.Generic,
            1,
            1,
            1,
            1,
            1,
            1
        );

        var item = new Item(serial)
        {
            ItemID = itemId,
            Hue = 0x1024,
            Amount = 10,
            Location = new Point3D(1000, 100, -10)
        };

        var expected = new WorldItemHS(item).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.ProtocolChanges = ProtocolChanges.StygianAbyss | ProtocolChanges.HighSeas;
        ns.SendWorldItem(item);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }
}
