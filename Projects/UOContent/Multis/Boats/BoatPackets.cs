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
using Server.Network;

namespace Server.Multis.Boats
{
    public static class BoatPackets
    {
        public static void SendMoveBoatHS(this NetState ns, Mobile beholder, BaseBoat boat,
            Direction d, int speed, BaseBoat.MovingEntitiesEnumerable ents, int xOffset, int yOffset)
        {
            if (ns?.HighSeas != true)
            {
                return;
            }

            const int minLength = 68; // 18 + 5 * 10
            var writer = new SpanWriter(stackalloc byte[minLength], true);
            writer.Write((byte)0xF6); // Packet ID
            writer.Seek(2, SeekOrigin.Current);

            writer.Write(boat.Serial);
            writer.Write((byte)speed);
            writer.Write((byte)d);
            writer.Write((byte)boat.Facing);
            writer.Write((short)(boat.X + xOffset));
            writer.Write((short)(boat.Y + yOffset));
            writer.Write((short)boat.Z);
            writer.Seek(2, SeekOrigin.Current); // count

            var count = 0;

            foreach (var ent in ents)
            {
                // If we assume that the entities list contains everything a player can see,
                // then this can be removed and the packet can be written once and copied to improve performance
                if (!beholder.CanSee(ent))
                {
                    continue;
                }

                writer.Write(ent.Serial);
                writer.Write((short)(ent.X + xOffset));
                writer.Write((short)(ent.Y + yOffset));
                writer.Write((short)ent.Z);
                ++count;
            }

            writer.Seek(16, SeekOrigin.Begin);
            writer.Write((short)count);
            writer.WritePacketLength();

            ns.Send(writer.Span);
        }

        public static void SendDisplayBoatHS(this NetState ns, Mobile beholder, BaseBoat boat)
        {
            if (ns?.HighSeas != true)
            {
                return;
            }

            var minLength = PacketContainerBuilder.MinPacketLength
                            + OutgoingEntityPackets.MaxWorldEntityPacketLength
                            * 5; // Minimum of boat, hold, planks, and the player

            using var builder = new PacketContainerBuilder(stackalloc byte[minLength]);

            Span<byte> buffer = builder.GetSpan(OutgoingEntityPackets.MaxWorldEntityPacketLength);

            foreach (var entity in boat.GetMovingEntities(true))
            {
                if (!beholder.CanSee(entity))
                {
                    continue;
                }

                buffer.InitializePacket();
                var bytesWritten = OutgoingEntityPackets.CreateWorldEntity(buffer, entity, true);
                builder.Advance(bytesWritten);
            }

            ns.Send(builder.Finalize());
        }
    }
}
