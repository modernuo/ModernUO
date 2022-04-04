/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: OutgoingEntityPackets.cs                                        *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Buffers;
using Server.Items;

namespace Server.Network;

public static class OutgoingEntityPackets
{
    public const int OPLPacketLength = 9;
    public const int RemoveEntityLength = 5;
    public const int MaxWorldEntityPacketLength = 26;

    public static void CreateOPLInfo(Span<byte> buffer, Item item) =>
        CreateOPLInfo(buffer, item.Serial, item.PropertyList.Hash);

    public static void CreateOPLInfo(Span<byte> buffer, Serial serial, int hash)
    {
        if (buffer[0] != 0)
        {
            return;
        }

        var writer = new SpanWriter(buffer);
        writer.Write((byte)0xDC); // Packet ID
        writer.Write(serial);
        writer.Write(hash);
    }

    public static void SendOPLInfo(this NetState ns, IPropertyListObject obj) =>
        ns.SendOPLInfo(obj.Serial, obj.PropertyList.Hash);

    public static void SendOPLInfo(this NetState ns, Serial serial, int hash)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        Span<byte> buffer = stackalloc byte[OPLPacketLength].InitializePacket();
        CreateOPLInfo(buffer, serial, hash);

        ns.Send(buffer);
    }

    public static void CreateRemoveEntity(Span<byte> buffer, Serial serial)
    {
        if (buffer[0] != 0)
        {
            return;
        }

        var writer = new SpanWriter(buffer);
        writer.Write((byte)0x1D); // Packet ID
        writer.Write(serial);
    }

    public static void SendRemoveEntity(this NetState ns, Serial serial)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        Span<byte> buffer = stackalloc byte[RemoveEntityLength].InitializePacket();
        CreateRemoveEntity(buffer, serial);

        ns.Send(buffer);
    }

    public static int CreateWorldEntity(Span<byte> buffer, IEntity entity, bool isHS)
    {
        if (buffer[0] != 0)
        {
            return buffer.Length;
        }

        var writer = new SpanWriter(buffer);
        writer.Write((byte)0xF3); // Packet ID
        writer.Write((short)0x1); // command

        int type = 0;
        int gfx = 0;
        int amount = 1;
        int hue = 0;
        byte light = 0;
        int flags = 0;

        if (entity is BaseMulti multi)
        {
            type = 2;
            gfx = multi.ItemID & (isHS ? 0xFFFF : 0x7FFF);
            hue = multi.Hue;
            amount = multi.Amount;
        }
        else if (entity is Item item)
        {
            // type = 3 if is damageable
            gfx = item.ItemID & (isHS ? 0xFFFF : 0x7FFF);
            hue = item.Hue;
            amount = item.Amount;
            light = (byte)item.Light;
            flags = item.GetPacketFlags();
        }
        else if (entity is Mobile mobile)
        {
            type = 1;
            gfx = mobile.Body;
            hue = mobile.Hue;
            flags = mobile.GetPacketFlags(true);
        }

        writer.Write((byte)type);
        writer.Write(entity.Serial);
        writer.Write((ushort)gfx);
        writer.Write((byte)0);

        writer.Write((short)amount); // Min
        writer.Write((short)amount); // Max

        writer.Write((short)(entity.X & 0x7FFF));
        writer.Write((short)(entity.Y & 0x3FFF));
        writer.Write((sbyte)entity.Z);

        writer.Write(light);
        writer.Write((short)hue);
        writer.Write((byte)flags);

        if (isHS)
        {
            writer.Write((short)0);
        }

        return writer.Position;
    }
}
