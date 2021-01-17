/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: EntityPackets.cs                                                *
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
using System.Collections.Generic;

namespace Server.Network
{
    public static class EntityPackets
    {
        public static void CreateBatchEntities(this NetState ns, List<IEntity> entities)
        {
            if (ns == null)
            {
                return;
            }

            bool isSA = ns.StygianAbyss;
            bool isHS = ns.HighSeas;

            var maxLength = PacketContainerBuilder.MinPacketLength
                            + OutgoingEntityPackets.MaxWorldEntityPacketLength
                            * entities.Count;

            using var builder = new PacketContainerBuilder(stackalloc byte[maxLength]);

            foreach (var entity in entities)
            {
                Span<byte> buffer = builder.GetSpan(OutgoingEntityPackets.MaxWorldEntityPacketLength).InitializePacket();
                var bytesWritten = OutgoingEntityPackets.CreateWorldEntity(buffer, entity, isSA, isHS);
                builder.Advance(bytesWritten);
            }

            ns.Send(builder.Finalize());
        }
    }
}
