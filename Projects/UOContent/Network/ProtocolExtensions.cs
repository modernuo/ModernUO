/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
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

namespace Server.Network
{
    public static class ProtocolExtensions
    {
        public static PacketHandler[] Register(byte packetId)
        {
            var packetHandlers = new PacketHandler[0x100];

            void DecodeBundledPacket(NetState state, CircularBufferReader reader, int packetLength)
            {
                int cmd = reader.ReadByte();

                PacketHandler ph = packetHandlers[cmd];

                if (ph == null)
                {
                    return;
                }

                if (ph.Ingame && state.Mobile == null)
                {
                    state.LogInfo("Sent in-game packet (0x{0:X2}x{1:X2}) before having been attached to a mobile", packetId, cmd);
                    state.Disconnect("Sent in-game packet before being attached to a mobile.");
                }
                else if (ph.Ingame && state.Mobile.Deleted)
                {
                    state.Disconnect(string.Empty);
                }
                else
                {
                    ph.OnReceive(state, reader, packetLength);
                }
            }

            IncomingPackets.Register(packetId, 0, false, DecodeBundledPacket);
            return packetHandlers;
        }
    }
}
