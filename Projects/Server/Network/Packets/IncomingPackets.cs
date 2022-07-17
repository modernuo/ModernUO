/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
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

using System.Runtime.CompilerServices;

namespace Server.Network;

public static class IncomingPackets
{
    private static readonly EncodedPacketHandler[] _encodedHandlers = new EncodedPacketHandler[0x100];

    public static PacketHandler[] Handlers { get; } = new PacketHandler[0x100];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Register(int packetID, int length, bool ingame,
        delegate*<NetState, CircularBufferReader, int, void> onReceive) =>
        Register(new PacketHandler(packetID, length, ingame, onReceive));

    public static void Register(PacketHandler packetHandler)
    {
        Handlers[packetHandler.PacketID] = packetHandler;
    }

    public static PacketHandler GetHandler(int packetID) => Handlers[packetID];

    public static unsafe void RegisterEncoded(
        int packetID, bool ingame, delegate*<NetState, IEntity, EncodedReader, void> onReceive
    )
    {
        if (packetID is >= 0 and < 0x100)
        {
            _encodedHandlers[packetID] = new EncodedPacketHandler(packetID, ingame, onReceive);
        }
    }

    public static EncodedPacketHandler GetEncodedHandler(int packetID) =>
        packetID is >= 0 and < 0x100 ? _encodedHandlers[packetID] : null;

    public static void RemoveEncodedHandler(int packetID)
    {
        if (packetID is >= 0 and < 0x100)
        {
            _encodedHandlers[packetID] = null;
        }
    }

    public static unsafe void RegisterThrottler(int packetID, delegate*<int, NetState, out bool, bool> t)
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
            0xF1 => true, // Freeshard Protocol
            0xF4 => true, // CrashReport
            _    => false
        };
    }
}
