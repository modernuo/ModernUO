/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: VendorSellPackets.cs                                            *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Collections.Generic;

namespace Server.Network
{
    public sealed class VendorSellList : Packet
    {
        public VendorSellList(Mobile shopkeeper, List<SellItemState> sis) : base(0x9E)
        {
            EnsureCapacity(256);

            Stream.Write(shopkeeper.Serial);

            Stream.Write((ushort)sis.Count);

            foreach (var state in sis)
            {
                Stream.Write(state.Item.Serial);
                Stream.Write((ushort)state.Item.ItemID);
                Stream.Write((ushort)state.Item.Hue);
                Stream.Write((ushort)state.Item.Amount);
                Stream.Write((ushort)state.Price);

                var name = string.IsNullOrWhiteSpace(state.Item.Name) ? state.Name ?? "" : state.Item.Name.Trim();

                Stream.Write((ushort)name.Length);
                Stream.WriteAsciiFixed(name, (ushort)name.Length);
            }
        }
    }

    public sealed class EndVendorSell : Packet
    {
        public EndVendorSell(Mobile vendor) : base(0x3B, 8)
        {
            Stream.Write((ushort)8); // length
            Stream.Write(vendor.Serial);
            Stream.Write((byte)0);
        }
    }
}
