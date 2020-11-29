using System;
using System.Buffers;
using Server.Items;
using Server.Network;
using Xunit;

namespace Server.Tests.Network
{
    public class ContainerPacketTests : IClassFixture<ServerFixture>
    {

        [Fact]
        public void TestContainerDisplay()
        {
            Serial serial = 0x1000;
            ushort gumpId = 100;

            var data = new ContainerDisplay(serial, gumpId).Compile();

            Span<byte> expectedData = stackalloc byte[7];
            var pos = 0;

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

            var data = new ContainerDisplayHS(serial, gumpId).Compile();

            Span<byte> expectedData = stackalloc byte[9];
            var pos = 0;

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

            var data = new DisplaySpellbook(serial).Compile();

            Span<byte> expectedData = stackalloc byte[7];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x24); // Packet ID
            expectedData.Write(ref pos, serial);
            expectedData.Write(ref pos, (ushort)0xFFFF); // Gump ID

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestDisplaySpellbookHS()
        {
            Serial serial = 0x1000;

            var data = new DisplaySpellbookHS(serial).Compile();

            Span<byte> expectedData = stackalloc byte[9];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x24); // Packet ID
            expectedData.Write(ref pos, serial);
            expectedData.Write(ref pos, (ushort)0xFFFF); // Gump ID
            expectedData.Write(ref pos, (ushort)0x7D);   // Max Items?

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestNewSpellbookContent()
        {
            Serial serial = 0x1000;
            ushort graphic = 100;
            ushort offset = 10;
            ulong content = 0x123456789ABCDEF0;

            var data = new NewSpellbookContent(serial, graphic, offset, content).Compile();

            Span<byte> expectedData = stackalloc byte[23];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0xBF);   // Packet ID
            expectedData.Write(ref pos, (ushort)0x17); // Length
            expectedData.Write(ref pos, (ushort)0x1B); // Sub-packet
            expectedData.Write(ref pos, (ushort)0x1);  // Command
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

            var data = new SpellbookContent(serial, offset, content).Compile();

            Span<byte> expectedData = stackalloc byte[5 + 64 * 19]; // Max size
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x3C); // Packet ID
            pos += 4;                                // Length + spell count

            ushort count = 0;

            for (var i = 0; i < 64; i++)
            {
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
            }

            expectedData.Slice(1, 2).Write((ushort)pos); // Length
            expectedData.Slice(3, 2).Write(count);       // Count

            expectedData = expectedData.Slice(0, pos);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestSpellbookContent6017()
        {
            Serial serial = 0x1000;
            ushort offset = 10;
            ulong content = 0x123456789ABCDEF0;

            var data = new SpellbookContent6017(serial, offset, content).Compile();

            Span<byte> expectedData = stackalloc byte[5 + 64 * 20]; // Max size
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x3C); // Packet ID
            pos += 4;                                // Length + spell count

            ushort count = 0;

            for (var i = 0; i < 64; i++)
            {
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
            }

            expectedData.Slice(1, 2).Write((ushort)pos); // Length
            expectedData.Slice(3, 2).Write(count);       // Count

            expectedData = expectedData.Slice(0, pos);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestContainerContentUpdate()
        {
            Serial serial = 0x1;
            var item = new Item(serial);

            var data = new ContainerContentUpdate(item).Compile();

            Span<byte> expectedData = stackalloc byte[20];
            var pos = 0;

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
            var item = new Item(serial);

            var data = new ContainerContentUpdate6017(item).Compile();

            Span<byte> expectedData = stackalloc byte[21];
            var pos = 0;

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
            var cont = new Container(World.NewItem);
            cont.AddItem(new Item(World.NewItem));

            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            var data = new ContainerContent(m, cont).Compile();

            Span<byte> expectedData = stackalloc byte[5 + cont.Items.Count * 19]; // Max Size
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x3C); // Packet ID
            pos += 4;                                // Length + Count

            ushort count = 0;

            var itemCount = cont.Items.Count;
            for (var i = 0; i < itemCount; i++)
            {
                var child = cont.Items[i];
                if (child.Deleted || !m.CanSee(child))
                {
                    continue;
                }

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
            expectedData.Slice(3, 2).Write(count);       // Count

            expectedData = expectedData.Slice(0, pos);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestContainerContent6017()
        {
            var cont = new Container(World.NewItem);
            cont.AddItem(new Item(World.NewItem));

            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            var data = new ContainerContent6017(m, cont).Compile();

            Span<byte> expectedData = stackalloc byte[5 + cont.Items.Count * 20];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x3C); // Packet ID
            pos += 4;                                // Length + Count

            ushort count = 0;

            var itemCount = cont.Items.Count;
            for (var i = 0; i < itemCount; i++)
            {
                var child = cont.Items[i];
                if (child.Deleted || !m.CanSee(child))
                {
                    continue;
                }

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
            expectedData.Slice(3, 2).Write(count);       // Count

            expectedData = expectedData.Slice(0, pos);

            AssertThat.Equal(data, expectedData);
        }
    }
}
