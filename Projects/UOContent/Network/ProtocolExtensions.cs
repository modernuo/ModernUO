/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ProtocolExtension.cs                                            *
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

namespace Server.Network
{
    public interface IProtocolExtensionsInfo
    {
        public int PacketId { get; }
    }

    public static class ProtocolExtensions<T> where T : struct, IProtocolExtensionsInfo
    {
        private static readonly PacketHandler[] packetHandlers = new PacketHandler[0x100];
        private static int packetId;

        public static unsafe PacketHandler[] Register(T info)
        {
            packetId = info.PacketId;
            IncomingPackets.Register(packetId, 0, false, &DecodeBundledPacket);

            return packetHandlers;
        }

        private static unsafe void DecodeBundledPacket(NetState state, SpanReader reader)
        {
            int cmd = reader.ReadByte();

            PacketHandler ph = packetHandlers[cmd];

            if (ph == null)
            {
                return;
            }

            var from = state.Mobile;

            if (ph.InGameOnly)
            {
                if (from == null)
                {
                    state.Disconnect($"Received packet 0x{packetId:X2}x{cmd:X2} before having been attached to a mobile.");
                    return;
                }

                if (from.Deleted)
                {
                    state.Disconnect($"Received packet 0x{packetId:X2}x{cmd:X2} after having been attached to a deleted mobile.");
                    return;
                }
            }

            if (ph.OutOfGameOnly && from?.Deleted == false)
            {
                state.Disconnect($"Received packet 0x{packetId:X2}x{cmd:X2} after having been attached to a mobile.");
                return;
            }

            ph.OnReceive(state, reader);
        }
    }
}
