/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
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

using System.Buffers;
using System.Collections.Generic;
using System.IO;
using Server.Network;

namespace Server.Multis.Boats
{
    public static class BoatPackets
    {
        public static void SendMoveBoatHS(this NetState ns, Mobile beholder, BaseBoat boat,
            Direction d, int speed, List<IEntity> ents, int xOffset, int yOffset)
        {
            if (ns == null)
            {
                return;
            }

            var maxLength = 18 + ents.Count * 10;
            var writer = new SpanWriter(stackalloc byte[maxLength]);
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
    }
}
