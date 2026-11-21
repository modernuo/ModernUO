/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
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
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Server.Logging;

namespace Server.Network;

public static class PacketUtilities
{
    private static readonly ILogger _logger = LogFactory.GetLogger(typeof(PacketUtilities));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WritePacketLength(this ref SpanWriter writer)
    {
        var length = writer.BytesWritten;

        if (length > ushort.MaxValue)
        {
            ReportOversizedPacket(writer.Span[0], length);
        }

        writer.Seek(1, SeekOrigin.Begin);
        writer.Write((ushort)length);
        writer.Seek(0, SeekOrigin.End);
    }

    // Always a bug in the packet builder, and the length field cannot express it: the client misreads the
    // stream from this packet on. The caller still sends, so RELEASE logs and DEBUG throws.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ReportOversizedPacket(byte packetId, int length)
    {
        _logger.Error(
            "Packet 0x{PacketId:X2} is {Length} bytes, over the {MaxLength} byte limit of its length field\n{StackTrace}",
            packetId,
            length,
            ushort.MaxValue,
            new StackTrace()
        );

#if DEBUG
        throw new InvalidOperationException(
            $"Packet 0x{packetId:X2} is {length} bytes, over the {ushort.MaxValue} byte limit of its length field."
        );
#endif
    }

    // If LOCAL INIT is off, then stack/heap allocations have garbage data
    // Initializes the first byte (Packet ID) so it can be used as a flag.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<byte> InitializePacket(this Span<byte> buffer)
    {
        buffer[0] = 0;
        return buffer;
    }
}
