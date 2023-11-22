/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BoatPackets.cs                                                  *
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
using System.IO;
using System.Runtime.CompilerServices;
using Server.Collections;
using Server.Network;

namespace Server.Multis.Boats;

public static class BoatPackets
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetMoveBoatHSPacketLength(int entityCount) => 18 + Math.Min(entityCount, 0xFFFF) * 10;

    private static void CreateMoveBoatHS(
        Span<byte> buffer, BaseBoat boat, PooledRefList<IEntity> entities,
        Direction d, int speed, int xOffset, int yOffset
    )
    {
        // Already initialized, so we don't have to create it again.
        if (buffer[0] != 0)
        {
            return;
        }

        var count = Math.Min(entities.Count, 0xFFFF);

        var writer = new SpanWriter(buffer);
        writer.Write((byte)0xF6); // Packet ID
        writer.Seek(2, SeekOrigin.Current); // Length

        writer.Write(boat.Serial);
        writer.Write((byte)speed);
        writer.Write((byte)d);
        writer.Write((byte)boat.Facing);
        writer.Write((short)(boat.X + xOffset));
        writer.Write((short)(boat.Y + yOffset));
        writer.Write((short)boat.Z);
        writer.Write((short)count);

        for (var i = 0; i < count; i++)
        {
            var ent = entities[i];

            writer.Write(ent.Serial);
            writer.Write((short)(ent.X + xOffset));
            writer.Write((short)(ent.Y + yOffset));
            writer.Write((short)ent.Z);
        }

        writer.WritePacketLength();
    }

    public static void SendMoveBoatHS(this NetState ns, BaseBoat boat,
        PooledRefList<IEntity> entities, Direction d, int speed, int xOffset, int yOffset)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        Span<byte> moveBoatPacket = stackalloc byte[GetMoveBoatHSPacketLength(entities.Count)]
            .InitializePacket();

        CreateMoveBoatHS(moveBoatPacket, boat, entities, d, speed, xOffset, yOffset);
        ns.Send(moveBoatPacket);
    }

    public static void SendMoveBoatHSUsingCache(this NetState ns, Span<byte> cache, BaseBoat boat,
        PooledRefList<IEntity> entities, Direction d, int speed, int xOffset, int yOffset)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        CreateMoveBoatHS(cache, boat, entities, d, speed, xOffset, yOffset);
        ns.Send(cache);
    }

    public static void SendDisplayBoatHS(this NetState ns, Mobile beholder, BaseBoat boat)
    {
        if (ns?.HighSeas != true || ns.CannotSendPackets())
        {
            return;
        }

        const int minLength = PacketContainerBuilder.MinPacketLength
                              + OutgoingEntityPackets.MaxWorldEntityPacketLength
                              * 5; // Minimum of boat, hold, planks, and the player

        using var builder = new PacketContainerBuilder(stackalloc byte[minLength]);

        foreach (var entity in boat.GetMovingEntities(true))
        {
            if (!beholder.CanSee(entity))
            {
                continue;
            }

            Span<byte> buffer = builder.GetSpan(OutgoingEntityPackets.MaxWorldEntityPacketLength).InitializePacket();
            var bytesWritten = OutgoingEntityPackets.CreateWorldEntity(buffer, entity, true);
            builder.Advance(bytesWritten);
        }

        ns.Send(builder.Finalize());
    }
}
