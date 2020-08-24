using System;
using System.Buffers;
using Server.Items;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
    public class ItemPacketTests : IClassFixture<ServerFixture>
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

            expectedData.Write(ref pos, (byte)0x1A);
            pos += 2; // Length

            if (item.Amount != 0)
                expectedData.Write(ref pos, serial | 0x80000000);
            else
                expectedData.Write(ref pos, serial & 0x7FFFFFFF);

            if (item is BaseMulti)
                expectedData.Write(ref pos, (ushort)(item.ItemID | 0x4000));
            else
                expectedData.Write(ref pos, (ushort)item.ItemID);

            if (item.Amount != 0)
                expectedData.Write(ref pos, (ushort)item.Amount);

            byte direction = (byte)item.Direction;
            ushort x = (ushort)(item.X & 0x7FFF);

            if (direction != 0)
                x |= 0x8000;

            expectedData.Write(ref pos, x);

            int hue = item.Hue;
            int flags = item.GetPacketFlags();
            ushort y = (ushort)(item.Y & 0x3FFF);

            if (hue != 0) y |= 0x8000;
            if (flags != 0) y |= 0x4000;

            expectedData.Write(ref pos, y);

            if (direction != 0)
                expectedData.Write(ref pos, direction);

            expectedData.Write(ref pos, (byte)item.Z);

            if (hue != 0)
                expectedData.Write(ref pos, (ushort)hue);

            if (flags != 0)
                expectedData.Write(ref pos, (byte)flags);

            // Length
            expectedData.Slice(1, 2).Write((ushort)pos);

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

            Span<byte> expectedData = stackalloc byte[24];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0xF3); // Packet ID
            expectedData.Write(ref pos, (ushort)0x1);
            expectedData.Write(ref pos, (byte)(isMulti ? 0x2 : 0x00)); // Item Type (Regular, or Multi)
            expectedData.Write(ref pos, item.Serial);
            expectedData.Write(ref pos, (ushort)(item.ItemID & (isMulti ? 0x3FFF : 0xFFFF)));

#if NO_LOCAL_INIT
      expectedData.Write(ref pos, (byte)0)
#else
            pos++;
#endif

            expectedData.Write(ref pos, (ushort)item.Amount); // Amount (min?)
            expectedData.Write(ref pos, (ushort)item.Amount); // Amount (max?)
            expectedData.Write(ref pos, loc); // X, Y, Z
            expectedData.Write(ref pos, (byte)item.Light); // Light
            expectedData.Write(ref pos, (ushort)item.Hue); // Hue
            expectedData.Write(ref pos, (byte)item.GetPacketFlags()); // Flags

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

            Span<byte> expectedData = stackalloc byte[26];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0xF3); // Packet ID
            expectedData.Write(ref pos, (ushort)0x1);
            expectedData.Write(ref pos, (byte)(isMulti ? 0x2 : 0x00)); // Item Type (Regular, or Multi)
            expectedData.Write(ref pos, item.Serial);
            expectedData.Write(ref pos, (ushort)(item.ItemID & (isMulti ? 0x3FFF : 0xFFFF)));

#if NO_LOCAL_INIT
      expectedData.Write(ref pos, (byte)0);
#else
            pos++;
#endif

            expectedData.Write(ref pos, (ushort)item.Amount); // Amount (min?)
            expectedData.Write(ref pos, (ushort)item.Amount); // Amount (max?)
            expectedData.Write(ref pos, loc); // X, Y, Z
            expectedData.Write(ref pos, (byte)item.Light); // Light
            expectedData.Write(ref pos, (ushort)item.Hue); // Hue
            expectedData.Write(ref pos, (byte)item.GetPacketFlags()); // Flags

#if NO_LOCAL_INIT
      expectedData.Write(ref pos, (ushort)0); // ??
#endif

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestContainerDisplay()
        {
            Serial serial = 0x1000;
            ushort gumpId = 100;

            Span<byte> data = new ContainerDisplay(serial, gumpId).Compile();

            Span<byte> expectedData = stackalloc byte[7];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0x24); // Packet ID
            expectedData.Write(ref pos, serial);
            expectedData.Write(ref pos, gumpId);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestContainerDisplayHS()
        {
            Serial serial = 0x1000;
            ushort gumpId = 100;

            Span<byte> data = new ContainerDisplayHS(serial, gumpId).Compile();

            Span<byte> expectedData = stackalloc byte[9];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0x24); // Packet ID
            expectedData.Write(ref pos, serial);
            expectedData.Write(ref pos, gumpId);
            expectedData.Write(ref pos, (ushort)0x7D); // Max Items?

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestDisplaySpellbook()
        {
            Serial serial = 0x1000;

            Span<byte> data = new DisplaySpellbook(serial).Compile();

            Span<byte> expectedData = stackalloc byte[7];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0x24); // Packet ID
            expectedData.Write(ref pos, serial);
            expectedData.Write(ref pos, (ushort)0xFFFF); // Gump ID

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestDisplaySpellbookHS()
        {
            Serial serial = 0x1000;

            Span<byte> data = new DisplaySpellbookHS(serial).Compile();

            Span<byte> expectedData = stackalloc byte[9];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0x24); // Packet ID
            expectedData.Write(ref pos, serial);
            expectedData.Write(ref pos, (ushort)0xFFFF); // Gump ID
            expectedData.Write(ref pos, (ushort)0x7D); // Max Items?

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

            Span<byte> expectedData = stackalloc byte[23];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0xBF); // Packet ID
            expectedData.Write(ref pos, (ushort)0x17); // Length
            expectedData.Write(ref pos, (ushort)0x1B); // Sub-packet
            expectedData.Write(ref pos, (ushort)0x1); // Command
            expectedData.Write(ref pos, serial);
            expectedData.Write(ref pos, graphic);
            expectedData.Write(ref pos, offset);
            expectedData.WriteLE(ref pos, content);

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

            expectedData.Write(ref pos, (byte)0x3C); // Packet ID
            pos += 4; // Length + spell count

            ushort count = 0;

            for (var i = 0; i < 64; i++)
                if ((content & (1ul << i)) != 0)
                {
                    expectedData.Write(ref pos, 0x7FFFFFFF - i);
#if NO_LOCAL_INIT
          expectedData.Write(ref pos, (ushort)0);
          expectedData.Write(ref pos, (byte)0);
#else
                    pos += 3;
#endif
                    expectedData.Write(ref pos, (ushort)(i + offset));
#if NO_LOCAL_INIT
          expectedData.Write(ref pos, 0); // X. Y
#else
                    pos += 4;
#endif
                    expectedData.Write(ref pos, serial);
#if NO_LOCAL_INIT
          expectedData.Write(ref pos, (ushort)0);
#else
                    pos += 2;
#endif
                    count++;
                }

            expectedData.Slice(1, 2).Write((ushort)pos); // Length
            expectedData.Slice(3, 2).Write(count); // Count

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

            Span<byte> expectedData = stackalloc byte[5 + 64 * 20]; // Max size
            int pos = 0;

            expectedData.Write(ref pos, (byte)0x3C); // Packet ID
            pos += 4; // Length + spell count

            ushort count = 0;

            for (var i = 0; i < 64; i++)
                if ((content & (1ul << i)) != 0)
                {
                    expectedData.Write(ref pos, 0x7FFFFFFF - i);
#if NO_LOCAL_INIT
          expectedData.Write(ref pos, (ushort)0);
          expectedData.Write(ref pos, (byte)0);
#else
                    pos += 3;
#endif
                    expectedData.Write(ref pos, (ushort)(i + offset));
#if NO_LOCAL_INIT
          expectedData.Write(ref pos, 0); // X. Y
          expectedData.Write(ref pos, (byte)0); // Grid Location
#else
                    pos += 5;
#endif
                    expectedData.Write(ref pos, serial);
#if NO_LOCAL_INIT
          expectedData.Write(ref pos, (ushort)0);
#else
                    pos += 2;
#endif
                    count++;
                }

            expectedData.Slice(1, 2).Write((ushort)pos); // Length
            expectedData.Slice(3, 2).Write(count); // Count

            expectedData = expectedData.Slice(0, pos);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestContainerContentUpdate()
        {
            Serial serial = 0x1;
            Item item = new Item(serial);

            Span<byte> data = new ContainerContentUpdate(item).Compile();

            Span<byte> expectedData = stackalloc byte[20];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0x25); // Packet ID
            expectedData.Write(ref pos, item.Serial);
            expectedData.Write(ref pos, (ushort)item.ItemID);
#if NO_LOCAL_INIT
      expectedData.Write(ref pos, (byte)0); // ItemID offset
#else
            pos++;
#endif
            expectedData.Write(ref pos, (ushort)Math.Min(item.Amount, ushort.MaxValue));
            expectedData.Write(ref pos, (ushort)item.X);
            expectedData.Write(ref pos, (ushort)item.Y);
            expectedData.Write(ref pos, item.Parent?.Serial ?? Serial.Zero);
            expectedData.Write(ref pos, (ushort)(item.QuestItem ? Item.QuestItemHue : item.Hue));

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestContainerContentUpdate6017()
        {
            Serial serial = 0x1;
            Item item = new Item(serial);

            Span<byte> data = new ContainerContentUpdate6017(item).Compile();

            Span<byte> expectedData = stackalloc byte[21];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0x25); // Packet ID
            expectedData.Write(ref pos, item.Serial);
            expectedData.Write(ref pos, (ushort)item.ItemID);
#if NO_LOCAL_INIT
      expectedData.Write(ref pos, (byte)0); // ItemID offset
#else
            pos++;
#endif
            expectedData.Write(ref pos, (ushort)Math.Min(item.Amount, ushort.MaxValue));
            expectedData.Write(ref pos, (ushort)item.X);
            expectedData.Write(ref pos, (ushort)item.Y);
#if NO_LOCAL_INIT
      expectedData.Write(ref pos, (byte)0); // Grid Location?
#else
            pos++;
#endif
            expectedData.Write(ref pos, item.Parent?.Serial ?? Serial.Zero);
            expectedData.Write(ref pos, (ushort)(item.QuestItem ? Item.QuestItemHue : item.Hue));

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestContainerContent()
        {
            Container cont = new Container(Serial.LastItem + 1);
            cont.AddItem(new Item(Serial.LastItem + 2));

            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            Span<byte> data = new ContainerContent(m, cont).Compile();

            Span<byte> expectedData = stackalloc byte[5 + cont.Items.Count * 19]; // Max Size
            int pos = 0;

            expectedData.Write(ref pos, (byte)0x3C); // Packet ID
            pos += 4; // Length + Count

            ushort count = 0;

            int itemCount = cont.Items.Count;
            for (var i = 0; i < itemCount; i++)
            {
                var child = cont.Items[i];
                if (child.Deleted || !m.CanSee(child))
                    continue;

                expectedData.Write(ref pos, child.Serial);
                expectedData.Write(ref pos, (ushort)child.ItemID);
#if NO_LOCAL_INIT
      expectedData.Write(ref pos, (byte)0); // ItemID offset
#else
                pos++;
#endif
                expectedData.Write(ref pos, (ushort)Math.Min(child.Amount, ushort.MaxValue));
                expectedData.Write(ref pos, (ushort)child.X);
                expectedData.Write(ref pos, (ushort)child.Y);
                expectedData.Write(ref pos, cont.Serial);
                expectedData.Write(ref pos, (ushort)(child.QuestItem ? Item.QuestItemHue : child.Hue));

                count++;
            }

            expectedData.Slice(1, 2).Write((ushort)pos); // Length
            expectedData.Slice(3, 2).Write(count); // Count

            expectedData = expectedData.Slice(0, pos);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestContainerContent6017()
        {
            Container cont = new Container(Serial.LastItem + 1);
            cont.AddItem(new Item(Serial.LastItem + 2));

            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            Span<byte> data = new ContainerContent6017(m, cont).Compile();

            Span<byte> expectedData = stackalloc byte[5 + cont.Items.Count * 20];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0x3C); // Packet ID
            pos += 4; // Length + Count

            ushort count = 0;

            int itemCount = cont.Items.Count;
            for (var i = 0; i < itemCount; i++)
            {
                var child = cont.Items[i];
                if (child.Deleted || !m.CanSee(child))
                    continue;

                expectedData.Write(ref pos, child.Serial);
                expectedData.Write(ref pos, (ushort)child.ItemID);
#if NO_LOCAL_INIT
      expectedData.Write(ref pos, (byte)0); // ItemID offset
#else
                pos++;
#endif
                expectedData.Write(ref pos, (ushort)Math.Min(child.Amount, ushort.MaxValue));
                expectedData.Write(ref pos, (ushort)child.X);
                expectedData.Write(ref pos, (ushort)child.Y);
#if NO_LOCAL_INIT
      expectedData.Write(ref pos, (byte)0); // Grid Location?
#else
                pos++;
#endif
                expectedData.Write(ref pos, cont.Serial);
                expectedData.Write(ref pos, (ushort)(child.QuestItem ? Item.QuestItemHue : child.Hue));

                count++;
            }

            expectedData.Slice(1, 2).Write((ushort)pos); // Length
            expectedData.Slice(3, 2).Write(count); // Count

            expectedData = expectedData.Slice(0, pos);

            AssertThat.Equal(data, expectedData);
        }
    }
}
