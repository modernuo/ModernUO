/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: OutgoingMovementPackets.cs                                      *
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
using System.Runtime.CompilerServices;

namespace Server.Network;

public enum SpeedControlSetting
{
    Disable,
    Mount,
    Walk
}

public static class OutgoingMovementPackets
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendSpeedControl(this NetState ns, SpeedControlSetting speedControl) =>
        ns?.Send(stackalloc byte[] { 0xBF, 0x00, 0x6, 0x00, 0x26, (byte)speedControl });

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendMovePlayer(this NetState ns, Direction d) => ns?.Send(stackalloc byte[] { 0x97, (byte)d });

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendMovementAck(this NetState ns, int seq, Mobile m) =>
        ns.SendMovementAck(seq, Notoriety.Compute(m, m));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendMovementAck(this NetState ns, int seq, int noto) =>
        ns?.Send(stackalloc byte[] { 0x22, (byte)seq, (byte)noto });

    public static void SendMovementRej(this NetState ns, int seq, Mobile m)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[8]);
        writer.Write((byte)0x21); // Packet ID
        writer.Write((byte)seq);
        writer.Write((short)m.X);
        writer.Write((short)m.Y);
        writer.Write((byte)m.Direction);
        writer.Write((sbyte)m.Z);

        ns.Send(writer.Span);
    }

    public static void SendInitialFastwalkStack(this NetState ns, uint[] keys)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[29]);
        writer.Write((byte)0xBF); // Packet ID
        writer.Write((ushort)29);
        writer.Write((ushort)0x1); // Subpacket
        writer.Write(keys[0]);
        writer.Write(keys[1]);
        writer.Write(keys[2]);
        writer.Write(keys[3]);
        writer.Write(keys[4]);
        writer.Write(keys[5]);

        ns.Send(writer.Span);
    }

    public static void SendFastwalkStackKey(this NetState ns, uint key = 0)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[9]);
        writer.Write((byte)0xBF); // Packet ID
        writer.Write((ushort)9);
        writer.Write((ushort)0x2); // Subpacket
        writer.Write(key);

        ns.Send(writer.Span);
    }

    public static void SendTimeSyncResponse(this NetState ns)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[25]);
        writer.Write((byte)0xF2); // Packet ID

        writer.Write(Core.TickCount); // ??
        writer.Write(Core.TickCount); // ??
        writer.Write(Core.TickCount); // ??

        ns.Send(writer.Span);
    }
}
