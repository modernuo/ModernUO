/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: IncomingPackets.cs                                              *
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
    public static class IncomingPackets
    {
        private static readonly PacketHandler[] m_6017Handlers = new PacketHandler[0x100];

        private static readonly EncodedPacketHandler[] m_EncodedHandlersLow = new EncodedPacketHandler[0x100];

        private static readonly Dictionary<int, EncodedPacketHandler> m_EncodedHandlersHigh =
            new();

        public static PacketHandler[] Handlers { get; } = new PacketHandler[0x100];

        public static void Register(int packetID, int length, bool ingame, OnPacketReceive onReceive)
        {
            Handlers[packetID] = new PacketHandler(packetID, length, ingame, onReceive);
            m_6017Handlers[packetID] ??= new PacketHandler(packetID, length, ingame, onReceive);
        }

        public static PacketHandler GetHandler(int packetID) => Handlers[packetID];

        public static void Register6017(int packetID, int length, bool ingame, OnPacketReceive onReceive)
        {
            m_6017Handlers[packetID] = new PacketHandler(packetID, length, ingame, onReceive);
        }

        public static PacketHandler Get6017Handler(int packetID) => m_6017Handlers[packetID];

        public static void RegisterEncoded(int packetID, bool ingame, OnEncodedPacketReceive onReceive)
        {
            if (packetID >= 0 && packetID < 0x100)
            {
                m_EncodedHandlersLow[packetID] = new EncodedPacketHandler(packetID, ingame, onReceive);
            }
            else
            {
                m_EncodedHandlersHigh[packetID] = new EncodedPacketHandler(packetID, ingame, onReceive);
            }
        }

        public static EncodedPacketHandler GetEncodedHandler(int packetID)
        {
            if (packetID >= 0 && packetID < 0x100)
            {
                return m_EncodedHandlersLow[packetID];
            }

            m_EncodedHandlersHigh.TryGetValue(packetID, out var handler);
            return handler;
        }

        public static void RemoveEncodedHandler(int packetID)
        {
            if (packetID >= 0 && packetID < 0x100)
            {
                m_EncodedHandlersLow[packetID] = null;
            }
            else
            {
                m_EncodedHandlersHigh.Remove(packetID);
            }
        }

        public static void RegisterThrottler(int packetID, ThrottlePacketCallback t)
        {
            var ph = GetHandler(packetID);

            if (ph != null)
            {
                ph.ThrottleCallback = t;
            }

            ph = Get6017Handler(packetID);

            if (ph != null)
            {
                ph.ThrottleCallback = t;
            }
        }

        public static int ProcessPacket(this NetState ns, ArraySegment<byte>[] buffer)
        {
            var reader = new CircularBufferReader(buffer);

            var packetId = reader.ReadByte();

            if (!ns.Seeded)
            {
                if (packetId == 0xEF)
                {
                    // new packet in client 6.0.5.0 replaces the traditional seed method with a seed packet
                    // 0xEF = 239 = multicast IP, so this should never appear in a normal seed. So this is backwards compatible with older clients.
                    ns.Seeded = true;
                }
                else
                {
                    var seed = (packetId << 24) | (reader.ReadByte() << 16) | (reader.ReadByte() << 8) | reader.ReadByte();

                    if (seed == 0)
                    {
                        ns.WriteConsole("Invalid client detected, disconnecting");
                        return -1;
                    }

                    ns.m_Seed = seed;
                    ns.Seeded = true;

                    return 4;
                }
            }

            if (ns.CheckEncrypted(packetId))
            {
                return -1;
            }

            // Get Handlers
            var handler = ns.GetHandler(packetId);

            if (handler == null)
            {
                reader.Trace(ns);
                return -1;
            }

            var packetLength = handler.Length;
            if (handler.Length <= 0 && reader.Length >= 3)
            {
                packetLength = reader.ReadUInt16();
                if (packetLength < 3)
                {
                    return -1;
                }
            }

            // Not enough data, let's wait for more to come in
            if (reader.Length < packetLength)
            {
                return 0;
            }

            if (handler.Ingame && ns.Mobile?.Deleted != false)
            {
                ns.WriteConsole("Sent ingame packet (0x{1:X2}) without being attached to a valid mobile.", ns, packetId);
                return -1;
            }

            var throttled = handler.ThrottleCallback?.Invoke(ns) ?? TimeSpan.Zero;

            if (throttled > TimeSpan.Zero)
            {
                ns.ThrottledUntil = DateTime.UtcNow + throttled;
            }

            handler.OnReceive(ns, reader);

            return packetLength;
        }
    }
}
