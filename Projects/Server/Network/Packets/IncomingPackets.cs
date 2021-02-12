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

using System.Collections.Generic;
using System.Runtime.CompilerServices;

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
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInfoPacket(byte packetId)
        {
            // These packets can arrive at any time during the login process. They're just informational.
            return packetId switch
            {
                0x01 => true, // Disconnect
                0x73 => true, // Ping
                0xA4 => true, // SystemInfo
                0xB1 => true, // Gump Response
                0xBB => true, // Account ID
                0xBD => true, // Client Version
                0xBE => true, // Assist Version
                0xD9 => true, // Hardware Info
                0xDD => true, // Gumps (Packed)
                0xE1 => true, // Client Type
                0xF4 => true, // CrashReport
                _    => false
            };
        }
    }
}
