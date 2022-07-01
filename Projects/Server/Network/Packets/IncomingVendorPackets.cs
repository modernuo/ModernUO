/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: IncomingVendorPackets.cs                                        *
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

namespace Server.Network;

public static class IncomingVendorPackets
{
    public static unsafe void Configure()
    {
        IncomingPackets.Register(0x3B, 0, true, &VendorBuyReply);
        IncomingPackets.Register(0x9F, 0, true, &VendorSellReply);
    }

    public static void VendorBuyReply(NetState state, CircularBufferReader reader, int packetLength)
    {
        var vendor = World.FindMobile((Serial)reader.ReadUInt32());

        if (vendor == null)
        {
            return;
        }

        var flag = reader.ReadByte();

        if (!vendor.Deleted && Utility.InRange(vendor.Location, state.Mobile.Location, 10) && flag == 0x02)
        {
            var msgSize = packetLength - 8; // Remaining bytes

            if (msgSize / 7 > 100)
            {
                return;
            }

            var buyList = new List<BuyItemResponse>(msgSize / 7);
            while (msgSize > 0)
            {
                var layer = reader.ReadByte();
                var serial = (Serial)reader.ReadUInt32();
                int amount = reader.ReadInt16();

                buyList.Add(new BuyItemResponse(serial, amount));
                msgSize -= 7;
            }

            if (buyList.Count <= 0 || (vendor as IVendor)?.OnBuyItems(state.Mobile, buyList) != true)
            {
                return;
            }
        }

        state.SendEndVendorBuy(vendor.Serial);
    }

    public static void VendorSellReply(NetState state, CircularBufferReader reader, int packetLength)
    {
        var serial = (Serial)reader.ReadUInt32();
        var vendor = World.FindMobile(serial);

        if (vendor == null)
        {
            return;
        }

        if (vendor.Deleted || !Utility.InRange(vendor.Location, state.Mobile.Location, 10))
        {
            state.SendEndVendorSell(vendor.Serial);
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
            var item = World.FindItem((Serial)reader.ReadUInt32());
            int amount = reader.ReadInt16();

            if (item != null && amount > 0)
            {
                sellList.Add(new SellItemResponse(item, amount));
            }
        }

        if (sellList.Count > 0 && vendor is IVendor v && v.OnSellItems(state.Mobile, sellList))
        {
            state.SendEndVendorSell(vendor.Serial);
        }
    }
}
