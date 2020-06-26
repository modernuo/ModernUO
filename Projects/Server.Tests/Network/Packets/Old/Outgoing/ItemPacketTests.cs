using System;
using Server.Items;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
  public class ItemPacketTests
  {
    [Fact]
    public void TestWorldItemPacket()
    {
      Serial serial = 0x1000;
      var itemId = 1;

      // Move to fixture
      TileData.ItemTable[itemId] = new ItemData(
        "Test Item Data", TileFlag.Generic, 1, 1, 1, 1, 1
      );

      Item item = new Item(serial)
      {
        ItemID = itemId,
        Hue = 0x1024,
        Amount = 10,
        Location = new Point3D(1000, 100, -10),
        Direction = Direction.Left
      };

      Span<byte> data = new WorldItem(item).Compile();

      Span<byte> expectedData = stackalloc byte[20]; // Max size
      int pos = 0;

      ((byte)0x1A).CopyTo(ref pos, expectedData);
      pos += 2; // Length

      if (item.Amount != 0)
        (serial | 0x80000000).CopyTo(ref pos, expectedData);
      else
        (serial & 0x7FFFFFFF).CopyTo(ref pos, expectedData);

      if (item is BaseMulti)
        ((ushort)(item.ItemID | 0x4000)).CopyTo(ref pos, expectedData);
      else
        ((ushort)item.ItemID).CopyTo(ref pos, expectedData);

      if (item.Amount != 0)
        ((ushort)item.Amount).CopyTo(ref pos, expectedData);

      byte direction = (byte)item.Direction;
      ushort x = (ushort)(item.X & 0x7FFF);

      if (direction != 0)
        x |= 0x8000;

      x.CopyTo(ref pos, expectedData);

      int hue = item.Hue;
      int flags = item.GetPacketFlags();
      ushort y = (ushort)(item.Y & 0x3FFF);

      if (hue != 0) y |= 0x8000;
      if (flags != 0) y |= 0x4000;

      y.CopyTo(ref pos, expectedData);

      if (direction != 0)
        direction.CopyTo(ref pos, expectedData);

      ((byte)item.Z).CopyTo(ref pos, expectedData);

      if (hue != 0)
        ((ushort)hue).CopyTo(ref pos, expectedData);

      if (flags != 0)
        ((byte)flags).CopyTo(ref pos, expectedData);

      // Length
      ((ushort)pos).CopyTo(expectedData.Slice(1, 2));

      // Slice the data to match in size
      data = data.Slice(0, pos);

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestWorldItemSAPacket()
    {
      Serial serial = 0x1000;
      ushort itemId = 1;

      // Move to fixture
      TileData.ItemTable[itemId] = new ItemData(
        "Test Item Data", TileFlag.Generic, 1, 1, 1, 1, 1
      );

      Item item = new Item(serial)
      {
        ItemID = itemId,
        Hue = 0x1024,
        Amount = 10,
        Location = new Point3D(1000, 100, -10)
      };

      var loc = item.Location;
      var isMulti = item is BaseMulti;

      Span<byte> data = new WorldItemSA(item).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0xF3, // Packet ID
        0x00, 0x01,
        (byte)(isMulti ? 0x02 : 0x00), // Item Type (Regular, or Multi)
        0x00, 0x00, 0x00, 0x00, // Serial
        0x00, 0x00, // Item ID
        0x00,
        0x00, 0x00, // Amount (min?)
        0x00, 0x00, // Amount (max?)
        0x00, 0x00, // X
        0x00, 0x00, // Y
        (byte)loc.Z, // Z
        (byte)item.Light, // Light
        0x00, 0x00, // Hue
        (byte)item.GetPacketFlags() // Flags
      };

      serial.CopyTo(expectedData.Slice(4, 4));
      ((ushort)(itemId & (isMulti ? 0x3FFF : 0xFFFF))).CopyTo(expectedData.Slice(8, 2));

      ushort amount = (ushort)item.Amount;
      amount.CopyTo(expectedData.Slice(11, 2));
      amount.CopyTo(expectedData.Slice(13, 2));

      ((ushort)loc.X).CopyTo(expectedData.Slice(15, 2));
      ((ushort)loc.Y).CopyTo(expectedData.Slice(17, 2));
      ((ushort)item.Hue).CopyTo(expectedData.Slice(21, 2));

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestWorldItemHSPacket()
    {
      Serial serial = 0x1000;
      var itemId = 1;

      // Move to fixture
      TileData.ItemTable[itemId] = new ItemData(
        "Test Item Data", TileFlag.Generic, 1, 1, 1, 1, 1
      );

      Item item = new Item(serial)
      {
        ItemID = itemId,
        Hue = 0x1024,
        Amount = 10,
        Location = new Point3D(1000, 100, -10)
      };

      var loc = item.Location;
      var isMulti = item is BaseMulti;

      Span<byte> data = new WorldItemHS(item).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0xF3, // Packet ID
        0x00, 0x01,
        (byte)(isMulti ? 0x02 : 0x00), // Item Type (Regular, or Multi)
        0x00, 0x00, 0x00, 0x00, // Serial
        0x00, 0x00, // Item ID
        0x00,
        0x00, 0x00, // Amount (min?)
        0x00, 0x00, // Amount (max?)
        0x00, 0x00, // X
        0x00, 0x00, // Y
        (byte)loc.Z, // Z
        (byte)item.Light, // Light
        0x00, 0x00, // Hue
        (byte)item.GetPacketFlags(), // Flags
        00, 00 // ???
      };

      serial.CopyTo(expectedData.Slice(4, 4));
      ((ushort)(itemId & (isMulti ? 0x3FFF : 0xFFFF))).CopyTo(expectedData.Slice(8, 2));

      ushort amount = (ushort)item.Amount;
      amount.CopyTo(expectedData.Slice(11, 2));
      amount.CopyTo(expectedData.Slice(13, 2));

      ((ushort)loc.X).CopyTo(expectedData.Slice(15, 2));
      ((ushort)loc.Y).CopyTo(expectedData.Slice(17, 2));
      ((ushort)item.Hue).CopyTo(expectedData.Slice(21, 2));

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestContainerDisplay()
    {
      Serial serial = 0x1000;
      ushort gumpId = 100;

      Span<byte> data = new ContainerDisplay(serial, gumpId).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0x24, // Packet ID
        0x00, 0x00, 0x00, 0x00, // Serial
        0x00, 0x00 // Gump ID
      };

      serial.CopyTo(expectedData.Slice(1, 4));
      gumpId.CopyTo(expectedData.Slice(5, 2));

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestContainerDisplayHS()
    {
      Serial serial = 0x1000;
      ushort gumpId = 100;

      Span<byte> data = new ContainerDisplayHS(serial, gumpId).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0x24, // Packet ID
        0x00, 0x00, 0x00, 0x00, // Serial
        0x00, 0x00, // Gump ID
        0x00, 0x7D // Max Items?
      };

      serial.CopyTo(expectedData.Slice(1, 4));
      gumpId.CopyTo(expectedData.Slice(5, 2));

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestDisplaySpellbook()
    {
      Serial serial = 0x1000;

      Span<byte> data = new DisplaySpellbook(serial).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0x24, // Packet ID
        0x00, 0x00, 0x00, 0x00, // Serial
        0xFF, 0xFF // Gump ID
      };

      serial.CopyTo(expectedData.Slice(1, 4));

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestDisplaySpellbookHS()
    {
      Serial serial = 0x1000;

      Span<byte> data = new DisplaySpellbookHS(serial).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0x24, // Packet ID
        0x00, 0x00, 0x00, 0x00, // Serial
        0xFF, 0xFF, // Gump ID
        0x00, 0x7D // Max Items?
      };

      serial.CopyTo(expectedData.Slice(1, 4));

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestNewSpellbookContent()
    {
      Serial serial = 0x1000;
      ushort graphic = 100;
      ushort offset = 10;
      ulong content = 0x123456789ABCDEF0;

      Span<byte> data = new NewSpellbookContent(serial, graphic, offset, content).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0xBF, // Packet ID
        0x00, 0x17, // Length
        0x00, 0x1B, // Sub-packet
        0x00, 0x01,
        0x00, 0x00, 0x00, 0x00, // Serial
        0x00, 0x00, // Graphic
        0x00, 0x00, // Offset
        0x00, 0x00, 0x00, 0x00, // Content
        0x00, 0x00, 0x00, 0x00 // Content
      };

      serial.CopyTo(expectedData.Slice(7, 4));
      graphic.CopyTo(expectedData.Slice(11, 2));
      offset.CopyTo(expectedData.Slice(13, 2));
      content.CopyToLE(expectedData.Slice(15, 8));

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestSpellbookContent()
    {
      Serial serial = 0x1000;
      ushort offset = 10;
      ulong content = 0x123456789ABCDEF0;

      Span<byte> data = new SpellbookContent(serial, offset, content).Compile();

      Span<byte> expectedData = stackalloc byte[5 + 64 * 19]; // Max size
      int pos = 0;

      ((byte)0x3C).CopyTo(ref pos, expectedData);
      pos += 4; // Length + spell count

      ushort count = 0;

      for (var i = 0; i < 64; i++)
        if ((content & (1ul << i)) != 0)
        {
          (0x7FFFFFFF - i).CopyTo(ref pos, expectedData);
          expectedData.Clear(ref pos, 3);
          ((ushort)(i + offset)).CopyTo(ref pos, expectedData);
          expectedData.Clear(ref pos, 4); // X, Y
          serial.CopyTo(ref pos, expectedData);
          expectedData.Clear(ref pos, 2);
          count++;
        }

      ((ushort)pos).CopyTo(expectedData.Slice(1, 2));
      count.CopyTo(expectedData.Slice(3, 2));

      expectedData = expectedData.Slice(0, pos);

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestSpellbookContent6017()
    {
      Serial serial = 0x1000;
      ushort offset = 10;
      ulong content = 0x123456789ABCDEF0;

      Span<byte> data = new SpellbookContent6017(serial, offset, content).Compile();

      Span<byte> expectedData = stackalloc byte[5 + 64 * 19]; // Max size
      int pos = 0;

      ((byte)0x3C).CopyTo(ref pos, expectedData);
      pos += 4; // Length + spell count

      ushort count = 0;

      for (var i = 0; i < 64; i++)
        if ((content & (1ul << i)) != 0)
        {
          (0x7FFFFFFF - i).CopyTo(ref pos, expectedData);
          expectedData.Clear(ref pos, 3);
          ((ushort)(i + offset)).CopyTo(ref pos, expectedData);
          expectedData.Clear(ref pos, 5); // X, Y, Grid Location
          serial.CopyTo(ref pos, expectedData);
          expectedData.Clear(ref pos, 2);
          count++;
        }

      ((ushort)pos).CopyTo(expectedData.Slice(1, 2));
      count.CopyTo(expectedData.Slice(3, 2));

      expectedData = expectedData.Slice(0, pos);

      AssertThat.Equal(data, expectedData);
    }
  }
}
