/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: OutgoingPlayerPackets.cs                                        *
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
using System.IO;
using System.Runtime.CompilerServices;

namespace Server.Network
{
    public enum LRReason : byte
    {
        CannotLift,
        OutOfRange,
        OutOfSight,
        TryToSteal,
        AreHolding,
        Inspecific
    }

    public static class OutgoingPlayerPackets
    {
        public static void SendStatLockInfo(this NetState ns, Mobile m)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0xBF); // Packet ID
            writer.Write((ushort)12);
            writer.Write((short)0x19);
            writer.Write((byte)2);
            writer.Write(m.Serial);
            writer.Write((byte)0);

            var lockBits = ((int)m.StrLock << 4) | ((int)m.DexLock << 2) | (int)m.IntLock;

            writer.Write((byte)lockBits);

            ns.Send(ref buffer, writer.Position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendChangeUpdateRange(this NetState ns, byte range)
        {
            ns?.Send(stackalloc byte[] { 0xC8, range });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendDeathStatus(this NetState ns, bool dead)
        {
            ns?.Send(stackalloc byte[] { 0x2C, dead ? 0 : 2 });
        }

        public static void SendToggleSpecialAbility(this NetState ns, int abilityId, bool active)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0xBF); // Packet ID
            writer.Write((ushort)8);
            writer.Write((short)0x25);
            writer.Write((short)abilityId);
            writer.Write(active);

            ns.Send(ref buffer, writer.Position);
        }

        public static void SendDisplayProfile(this NetState ns, Serial m, string header, string body, string footer)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            header ??= "";
            body ??= "";
            footer ??= "";

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0xB8); // Packet ID
            writer.Seek(2, SeekOrigin.Current);
            writer.Write(m);
            writer.WriteAsciiNull(header);
            writer.WriteBigUniNull(footer);
            writer.WriteBigUniNull(body);

            writer.WritePacketLength();
            ns.Send(ref buffer, writer.Position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendLiftReject(this NetState ns, LRReason reason)
        {
            ns?.Send(stackalloc byte[] { 0x27, (byte)reason });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendLogoutAck(this NetState ns)
        {
            ns?.Send(stackalloc byte[] { 0xD1, 0x01 });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendWeather(this NetState ns, byte type, byte density, byte temp)
        {
            ns?.Send(stackalloc byte[] { 0x65, type, density, temp });
        }
    }
}
