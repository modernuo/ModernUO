using System;
using Server.Network;
using Xunit;

namespace Server.Tests.Network;

[Collection("Sequential Server Tests")]
public class ItemPacketTests : IDisposable
{
    private const int TestItemId = 1;

    // TileData is global and holds real client data when client files are loaded, so each test restores it.
    private readonly ItemData _originalItemData;

    public ItemPacketTests()
    {
        _originalItemData = TileData.ItemTable[TestItemId];
        TileData.ItemTable[TestItemId] = new ItemData("Test Item Data", TileFlag.Generic, 1, 1, 1, 1, 1, 1);
    }

    public void Dispose() => TileData.ItemTable[TestItemId] = _originalItemData;

    [Fact]
    public void TestWorldItemPacket()
    {
        var serial = (Serial)0x1024;

        var item = new Item(serial)
        {
            ItemID = TestItemId,
            Hue = 0x1024,
            Amount = 10,
            Location = new Point3D(1000, 100, -10),
            Direction = Direction.Left
        };

        PacketAssert.Sends(new WorldItem(item), ns => ns.SendWorldItem(item));
    }

    [Fact]
    public void TestWorldItemSAPacket()
    {
        var serial = (Serial)0x1024;

        var item = new Item(serial)
        {
            ItemID = TestItemId,
            Hue = 0x1024,
            Amount = 10,
            Location = new Point3D(1000, 100, -10)
        };

        PacketAssert.Sends(new WorldItemSA(item), ns => ns.SendWorldItem(item), ProtocolChanges.StygianAbyss);
    }

    [Fact]
    public void TestWorldItemHSPacket()
    {
        var serial = (Serial)0x1024;

        var item = new Item(serial)
        {
            ItemID = TestItemId,
            Hue = 0x1024,
            Amount = 10,
            Location = new Point3D(1000, 100, -10)
        };

        PacketAssert.Sends(
            new WorldItemHS(item),
            ns => ns.SendWorldItem(item),
            ProtocolChanges.StygianAbyss | ProtocolChanges.HighSeas
        );
    }
}
