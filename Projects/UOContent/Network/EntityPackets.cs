/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
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
using System.Runtime.CompilerServices;

namespace Server.Network
{
    public static class EntityPackets
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendBatchEntities(this NetState ns, IReadOnlyCollection<IEntity> entities) =>
            ns.SendBatchEntities(entities, entities.Count);

        public static void SendBatchEntities(this NetState ns, IEnumerable<IEntity> entities, int estimatedCount)
        {
            if (ns?.HighSeas != true || ns.CannotSendPackets())
            {
                return;
            }

            var minLength = PacketContainerBuilder.MinPacketLength
                            + OutgoingEntityPackets.MaxWorldEntityPacketLength
                            * estimatedCount;

            using var builder = new PacketContainerBuilder(stackalloc byte[minLength]);

            Span<byte> buffer = builder.GetSpan(OutgoingEntityPackets.MaxWorldEntityPacketLength);

            foreach (var entity in entities)
            {
                buffer.InitializePacket();
                var bytesWritten = OutgoingEntityPackets.CreateWorldEntity(buffer, entity, true);
                builder.Advance(bytesWritten);
            }

            ns.Send(builder.Finalize());
        }
    }
}
