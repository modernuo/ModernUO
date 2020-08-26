/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: SecureTradePackets.cs                                           *
 * Created: 2020/05/03 - Updated: 2020/05/03                             *
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

using Server.Items;

namespace Server.Network
{
    public sealed class DisplaySecureTrade : Packet
    {
        public DisplaySecureTrade(Mobile them, Container first, Container second, string name)
            : base(0x6F)
        {
            name ??= "";

            EnsureCapacity(18 + name.Length);

            Stream.Write((byte)0); // Display
            Stream.Write(them.Serial);
            Stream.Write(first.Serial);
            Stream.Write(second.Serial);
            Stream.Write(true);

            Stream.WriteAsciiFixed(name, 30);
        }
    }

    public sealed class CloseSecureTrade : Packet
    {
        public CloseSecureTrade(Container cont)
            : base(0x6F)
        {
            EnsureCapacity(8);

            Stream.Write((byte)1); // Close
            Stream.Write(cont.Serial);
        }
    }

    public enum TradeFlag : byte
    {
        Display = 0x0,
        Close = 0x1,
        Update = 0x2,
        UpdateGold = 0x3,
        UpdateLedger = 0x4
    }

    public sealed class UpdateSecureTrade : Packet
    {
        public UpdateSecureTrade(Container cont, bool first, bool second)
            : this(cont, TradeFlag.Update, first ? 1 : 0, second ? 1 : 0)
        {
        }

        public UpdateSecureTrade(Container cont, TradeFlag flag, int first, int second)
            : base(0x6F)
        {
            EnsureCapacity(17);

            Stream.Write((byte)flag);
            Stream.Write(cont.Serial);
            Stream.Write(first);
            Stream.Write(second);
        }
    }

    public sealed class SecureTradeEquip : Packet
    {
        public SecureTradeEquip(Item item, Mobile m) : base(0x25, 20)
        {
            Stream.Write(item.Serial);
            Stream.Write((short)item.ItemID);
            Stream.Write((byte)0);
            Stream.Write((short)item.Amount);
            Stream.Write((short)item.X);
            Stream.Write((short)item.Y);
            Stream.Write(m.Serial);
            Stream.Write((short)item.Hue);
        }
    }

    public sealed class SecureTradeEquip6017 : Packet
    {
        public SecureTradeEquip6017(Item item, Mobile m) : base(0x25, 21)
        {
            Stream.Write(item.Serial);
            Stream.Write((short)item.ItemID);
            Stream.Write((byte)0);
            Stream.Write((short)item.Amount);
            Stream.Write((short)item.X);
            Stream.Write((short)item.Y);
            Stream.Write((byte)0); // Grid Location?
            Stream.Write(m.Serial);
            Stream.Write((short)item.Hue);
        }
    }
}
