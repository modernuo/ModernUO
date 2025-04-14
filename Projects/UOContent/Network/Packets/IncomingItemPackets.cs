/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: IncomingItemPackets.cs                                          *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Buffers;
using System.Collections.Generic;
using System.IO;
using Server.Collections;
using Server.Items;
using Server.Mobiles;

namespace Server.Network;

public static class IncomingItemPackets
{
    public static unsafe void Configure()
    {
        IncomingPackets.Register(0x07, 7, true, &LiftReq);
        IncomingPackets.Register(new ContainerGridPacketHandler(0x08, 14, &DropReq));
        IncomingPackets.Register(0x13, 10, true, &EquipReq);
        IncomingPackets.Register(0xEC, 0, false, &EquipMacro);
        IncomingPackets.Register(0xED, 0, false, &UnequipMacro);
    }

    public static void LiftReq(NetState state, SpanReader reader)
    {
        var serial = (Serial)reader.ReadUInt32();
        int amount = reader.ReadUInt16();
        var item = World.FindItem(serial);

        state.Mobile.Lift(item, amount, out _, out _);
    }

    public static void EquipReq(NetState state, SpanReader reader)
    {
        var from = state.Mobile;
        var item = from.Holding;

        var valid = item != null && item.HeldBy == from && item.Map == Map.Internal;

        from.Holding = null;

        if (!valid)
        {
            return;
        }

        reader.Seek(5, SeekOrigin.Current);
        var to = World.FindMobile((Serial)reader.ReadUInt32()) ?? from;

        if (!to.AllowEquipFrom(from) || !to.EquipItem(item))
        {
            item.Bounce(from);
        }

        item.ClearBounce();
    }

    public static void DropReq(NetState state, SpanReader reader)
    {
        reader.ReadInt32(); // serial, ignored
        int x = reader.ReadInt16();
        int y = reader.ReadInt16();
        int z = reader.ReadSByte();

        if (state.ContainerGridLines)
        {
            reader.ReadByte(); // Grid Location?
        }

        Serial dest = (Serial)reader.ReadUInt32();

        var loc = new Point3D(x, y, z);

        var from = state.Mobile;

        if (dest.IsMobile)
        {
            from.Drop(World.FindMobile(dest), loc);
        }
        else if (dest.IsItem)
        {
            var item = World.FindItem(dest);

            if (item is BaseMulti multi && multi.AllowsRelativeDrop)
            {
                loc.X += multi.X;
                loc.Y += multi.Y;
                from.Drop(loc);
            }
            else
            {
                from.Drop(item, loc);
            }
        }
        else
        {
            from.Drop(loc);
        }
    }

    public static void EquipMacro(NetState state, SpanReader reader)
    {
        int count = reader.ReadByte();
        var serialList = PooledRefList<Serial>.Create(count);
        for (var i = 0; i < count; ++i)
        {
            serialList.Add((Serial)reader.ReadUInt32());
        }

        PlayerMobile.EquipMacro(state.Mobile, ref serialList);
        serialList.Dispose();
    }

    public static void UnequipMacro(NetState state, SpanReader reader)
    {
        int count = reader.ReadByte();
        var layers = PooledRefList<Layer>.Create(count);
        for (var i = 0; i < count; ++i)
        {
            layers.Add((Layer)reader.ReadUInt16());
        }

        PlayerMobile.UnequipMacro(state.Mobile, ref layers);
    }
}
