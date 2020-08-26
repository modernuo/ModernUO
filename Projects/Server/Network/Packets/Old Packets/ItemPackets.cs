/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: ItemPackets.cs - Created: 2020/05/26 - Updated: 2020/05/26      *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * This program is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 * GNU General Public License for more details.                          *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.IO;
using Server.Items;

namespace Server.Network
{
    public sealed class WorldItem : Packet
    {
        public WorldItem(Item item) : base(0x1A)
        {
            EnsureCapacity(20);

            // 14 base length
            // +2 - Amount
            // +2 - Hue
            // +1 - Flags

            var serial = item.Serial.Value;
            var itemID = item.ItemID & 0x3FFF;
            var amount = item.Amount;
            var loc = item.Location;
            var x = loc.m_X;
            var y = loc.m_Y;
            var hue = item.Hue;
            var flags = item.GetPacketFlags();
            var direction = (int)item.Direction;

            if (amount != 0)
                serial |= 0x80000000;
            else
                serial &= 0x7FFFFFFF;

            Stream.Write(serial);

            if (item is BaseMulti)
                Stream.Write((short)(itemID | 0x4000));
            else
                Stream.Write((short)itemID);

            if (amount != 0) Stream.Write((short)amount);

            x &= 0x7FFF;

            if (direction != 0) x |= 0x8000;

            Stream.Write((short)x);

            y &= 0x3FFF;

            if (hue != 0) y |= 0x8000;

            if (flags != 0) y |= 0x4000;

            Stream.Write((short)y);

            if (direction != 0)
                Stream.Write((byte)direction);

            Stream.Write((sbyte)loc.m_Z);

            if (hue != 0)
                Stream.Write((ushort)hue);

            if (flags != 0)
                Stream.Write((byte)flags);
        }
    }

    public sealed class WorldItemSA : Packet
    {
        public WorldItemSA(Item item) : base(0xF3, 24)
        {
            Stream.Write((short)0x1);

            var itemID = item.ItemID;

            if (item is BaseMulti)
            {
                Stream.Write((byte)0x02);

                Stream.Write(item.Serial);

                itemID &= 0x3FFF;

                Stream.Write((short)itemID);

                Stream.Write((byte)0);
            }
            else
            {
                Stream.Write((byte)0x00);

                Stream.Write(item.Serial);

                itemID &= 0x7FFF;

                Stream.Write((short)itemID);

                Stream.Write((byte)0);
            }

            var amount = item.Amount;
            Stream.Write((short)amount);
            Stream.Write((short)amount);

            var loc = item.Location;
            Stream.Write((short)loc.m_X);
            Stream.Write((short)loc.m_Y);
            Stream.Write((sbyte)loc.m_Z);

            Stream.Write((byte)item.Light);
            Stream.Write((short)item.Hue);
            Stream.Write((byte)item.GetPacketFlags());
        }
    }

    public sealed class WorldItemHS : Packet
    {
        public WorldItemHS(Item item) : base(0xF3, 26)
        {
            Stream.Write((short)0x1);

            var itemID = item.ItemID;

            if (item is BaseMulti)
            {
                Stream.Write((byte)0x02);

                Stream.Write(item.Serial);

                itemID &= 0x3FFF;

                Stream.Write((ushort)itemID);

                Stream.Write((byte)0);
            }
            else
            {
                Stream.Write((byte)0x00);

                Stream.Write(item.Serial);

                itemID &= 0xFFFF;

                Stream.Write((ushort)itemID);

                Stream.Write((byte)0);
            }

            var amount = item.Amount;
            Stream.Write((short)amount);
            Stream.Write((short)amount);

            var loc = item.Location;
            Stream.Write((short)loc.m_X);
            Stream.Write((short)loc.m_Y);
            Stream.Write((sbyte)loc.m_Z);

            Stream.Write((byte)item.Light);
            Stream.Write((short)item.Hue);
            Stream.Write((byte)item.GetPacketFlags());

            Stream.Write((short)0x00); // ??
        }
    }

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
                Stream.Write((byte)(content >> (i * 8)));
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
                    Stream.Write((short)loc.m_X);
                    Stream.Write((short)loc.m_Y);
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
                    Stream.Write((short)loc.m_X);
                    Stream.Write((short)loc.m_Y);
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
