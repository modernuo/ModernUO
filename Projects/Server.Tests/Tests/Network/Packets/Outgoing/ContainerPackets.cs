using System;
using System.IO;

namespace Server.Network
{
    public sealed class DisplaySpellbook : Packet
    {
        public DisplaySpellbook(Serial book) : base(0x24, 7)
        {
            Stream.Write(book);
            Stream.Write((short)-1);
        }
    }

    public sealed class DisplaySpellbookHS : Packet
    {
        public DisplaySpellbookHS(Serial book) : base(0x24, 9)
        {
            Stream.Write(book);
            Stream.Write((short)-1);
            Stream.Write((short)0x7D);
        }
    }

    public sealed class NewSpellbookContent : Packet
    {
        public NewSpellbookContent(Serial spellbook, int graphic, int offset, ulong content) : base(0xBF)
        {
            EnsureCapacity(23);

            Stream.Write((short)0x1B);
            Stream.Write((short)0x01);

            Stream.Write(spellbook);
            Stream.Write((short)graphic);
            Stream.Write((short)offset);

            for (var i = 0; i < 8; ++i)
            {
                Stream.Write((byte)(content >> (i * 8)));
            }
        }
    }

    public sealed class SpellbookContent : Packet
    {
        public SpellbookContent(Serial spellbook, int offset, ulong content) : base(0x3C)
        {
            EnsureCapacity(5 + 64 * 19);

            var written = 0;

            Stream.Write((ushort)0);

            ulong mask = 1;

            for (var i = 0; i < 64; ++i, mask <<= 1)
            {
                if ((content & mask) != 0)
                {
                    Stream.Write(0x7FFFFFFF - i);
                    Stream.Write((ushort)0);
                    Stream.Write((byte)0);
                    Stream.Write((ushort)(i + offset));
                    Stream.Write((short)0);
                    Stream.Write((short)0);
                    Stream.Write(spellbook);
                    Stream.Write((short)0);

                    ++written;
                }
            }

            Stream.Seek(3, SeekOrigin.Begin);
            Stream.Write((ushort)written);
        }
    }

    public sealed class SpellbookContent6017 : Packet
    {
        public SpellbookContent6017(Serial spellbook, int offset, ulong content) : base(0x3C)
        {
            EnsureCapacity(5 + 64 * 20);

            var written = 0;

            Stream.Write((ushort)0);

            ulong mask = 1;

            for (var i = 0; i < 64; ++i, mask <<= 1)
            {
                if ((content & mask) != 0)
                {
                    Stream.Write(0x7FFFFFFF - i);
                    Stream.Write((ushort)0);
                    Stream.Write((byte)0);
                    Stream.Write((ushort)(i + offset));
                    Stream.Write((short)0);
                    Stream.Write((short)0);
                    Stream.Write((byte)0); // Grid Location?
                    Stream.Write(spellbook);
                    Stream.Write((short)0);

                    ++written;
                }
            }

            Stream.Seek(3, SeekOrigin.Begin);
            Stream.Write((ushort)written);
        }
    }

    public sealed class ContainerDisplay : Packet
    {
        public ContainerDisplay(Serial cont, int gumpId) : base(0x24, 7)
        {
            Stream.Write(cont);
            Stream.Write((short)gumpId);
        }
    }

    public sealed class ContainerDisplayHS : Packet
    {
        public ContainerDisplayHS(Serial cont, int gumpId) : base(0x24, 9)
        {
            Stream.Write(cont);
            Stream.Write((short)gumpId);
            Stream.Write((short)0x7D);
        }
    }

    public sealed class ContainerContentUpdate : Packet
    {
        public ContainerContentUpdate(Item item) : base(0x25, 20)
        {
            Serial parentSerial;

            if (item.Parent is Item parentItem)
            {
                parentSerial = parentItem.Serial;
            }
            else
            {
                Console.WriteLine("Warning: ContainerContentUpdate on item with !(parent is Item)");
                parentSerial = Serial.Zero;
            }

            Stream.Write(item.Serial);
            Stream.Write((ushort)item.ItemID);
            Stream.Write((byte)0); // signed, itemID offset
            Stream.Write((ushort)Math.Min(item.Amount, ushort.MaxValue));
            Stream.Write((short)item.X);
            Stream.Write((short)item.Y);
            Stream.Write(parentSerial);
            Stream.Write((ushort)(item.QuestItem ? Item.QuestItemHue : item.Hue));
        }
    }

    public sealed class ContainerContentUpdate6017 : Packet
    {
        public ContainerContentUpdate6017(Item item) : base(0x25, 21)
        {
            Serial parentSerial;

            if (item.Parent is Item parentItem)
            {
                parentSerial = parentItem.Serial;
            }
            else
            {
                Console.WriteLine("Warning: ContainerContentUpdate on item with !(parent is Item)");
                parentSerial = Serial.Zero;
            }

            Stream.Write(item.Serial);
            Stream.Write((ushort)item.ItemID);
            Stream.Write((byte)0); // signed, itemID offset
            Stream.Write((ushort)Math.Min(item.Amount, ushort.MaxValue));
            Stream.Write((short)item.X);
            Stream.Write((short)item.Y);
            Stream.Write((byte)0); // Grid Location?
            Stream.Write(parentSerial);
            Stream.Write((ushort)(item.QuestItem ? Item.QuestItemHue : item.Hue));
        }
    }

    public sealed class ContainerContent : Packet
    {
        public ContainerContent(Mobile beholder, Item beheld) : base(0x3C)
        {
            var items = beheld.Items;
            var count = items.Count;

            EnsureCapacity(5 + count * 19);

            var pos = Stream.Position;

            var written = 0;

            Stream.Write((ushort)0);

            for (var i = 0; i < count; ++i)
            {
                var child = items[i];

                if (!child.Deleted && beholder.CanSee(child))
                {
                    var loc = child.Location;

                    Stream.Write(child.Serial);
                    Stream.Write((ushort)child.ItemID);
                    Stream.Write((byte)0); // signed, itemID offset
                    Stream.Write((ushort)Math.Min(child.Amount, ushort.MaxValue));
                    Stream.Write((short)loc.X);
                    Stream.Write((short)loc.Y);
                    Stream.Write(beheld.Serial);
                    Stream.Write((ushort)(child.QuestItem ? Item.QuestItemHue : child.Hue));

                    ++written;
                }
            }

            Stream.Seek(pos, SeekOrigin.Begin);
            Stream.Write((ushort)written);
        }
    }

    public sealed class ContainerContent6017 : Packet
    {
        public ContainerContent6017(Mobile beholder, Item beheld) : base(0x3C)
        {
            var items = beheld.Items;
            var count = items.Count;

            EnsureCapacity(5 + count * 20);

            var pos = Stream.Position;

            var written = 0;

            Stream.Write((ushort)0);

            for (var i = 0; i < count; ++i)
            {
                var child = items[i];

                if (!child.Deleted && beholder.CanSee(child))
                {
                    var loc = child.Location;

                    Stream.Write(child.Serial);
                    Stream.Write((ushort)child.ItemID);
                    Stream.Write((byte)0); // signed, itemID offset
                    Stream.Write((ushort)Math.Min(child.Amount, ushort.MaxValue));
                    Stream.Write((short)loc.X);
                    Stream.Write((short)loc.Y);
                    Stream.Write((byte)0); // Grid Location?
                    Stream.Write(beheld.Serial);
                    Stream.Write((ushort)(child.QuestItem ? Item.QuestItemHue : child.Hue));

                    ++written;
                }
            }

            Stream.Seek(pos, SeekOrigin.Begin);
            Stream.Write((ushort)written);
        }
    }
}
