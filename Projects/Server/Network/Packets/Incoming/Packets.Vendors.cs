/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Packets.Vendors.cs                                              *
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
    public static partial class Packets
    {
        public static void VendorBuyReply(this NetState state, CircularBufferReader reader)
        {
            var vendor = World.FindMobile(reader.ReadUInt32());
            var flag = reader.ReadByte();

            if (vendor == null)
            {
                return;
            }

            if (vendor.Deleted || !Utility.RangeCheck(vendor.Location, state.Mobile.Location, 10))
            {
                state.Send(new EndVendorBuy(vendor));
                return;
            }

            if (flag == 0x02)
            {
                var msgSize = reader.Remaining;

                if (msgSize / 7 > 100)
                {
                    return;
                }

                var buyList = new List<BuyItemResponse>(msgSize / 7);
                while (msgSize > 0)
                {
                    var layer = reader.ReadByte();
                    Serial serial = reader.ReadUInt32();
                    int amount = reader.ReadInt16();

                    buyList.Add(new BuyItemResponse(serial, amount));
                    msgSize -= 7;
                }

                if (buyList.Count > 0 && vendor is IVendor v && v.OnBuyItems(state.Mobile, buyList))
                {
                    state.Send(new EndVendorBuy(vendor));
                }
            }
            else
            {
                state.Send(new EndVendorBuy(vendor));
            }
        }

        public static void VendorSellReply(this NetState state, CircularBufferReader reader)
        {
            Serial serial = reader.ReadUInt32();
            var vendor = World.FindMobile(serial);

            if (vendor == null)
            {
                return;
            }

            if (vendor.Deleted || !Utility.RangeCheck(vendor.Location, state.Mobile.Location, 10))
            {
                state.Send(new EndVendorSell(vendor));
                return;
            }

            int count = reader.ReadUInt16();

            if (count >= 100 || reader.Remaining != count * 6)
            {
                return;
            }

            var sellList = new List<SellItemResponse>(count);

            for (var i = 0; i < count; i++)
            {
                var item = World.FindItem(reader.ReadUInt32());
                int amount = reader.ReadInt16();

                if (item != null && amount > 0)
                {
                    sellList.Add(new SellItemResponse(item, amount));
                }
            }

            if (sellList.Count > 0 && vendor is IVendor v && v.OnSellItems(state.Mobile, sellList))
            {
                state.Send(new EndVendorSell(vendor));
            }
        }
    }
}
