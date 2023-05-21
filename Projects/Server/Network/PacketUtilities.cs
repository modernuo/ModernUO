/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: PacketUtilities.cs                                              *
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

namespace Server.Network;

public static class PacketUtilities
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WritePacketLength(this ref CircularBufferWriter writer)
    {
        var length = writer.Position;
        writer.Seek(1, SeekOrigin.Begin);
        writer.Write((ushort)length);
        writer.Seek(length, SeekOrigin.Begin);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WritePacketLength(this ref SpanWriter writer)
    {
        writer.Seek(1, SeekOrigin.Begin);
        writer.Write((ushort)writer.BytesWritten);
        writer.Seek(0, SeekOrigin.End);
    }

    // If LOCAL INIT is off, then stack/heap allocations have garbage data
    // Initializes the first byte (Packet ID) so it can be used as a flag.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<byte> InitializePacket(this Span<byte> buffer)
    {
#if NO_LOCAL_INIT
            if (buffer != null)
            {
                buffer[0] = 0;
            }
#endif
        return buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<byte> InitializePackets(this Span<byte> buffer, int width)
    {
#if NO_LOCAL_INIT
            for (var i = 0; i < buffer.Length; i += width)
            {
                buffer[i] = 0;
            }
#endif
        return buffer;
    }
}
