/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: OutgoingMobilePackets.cs                                        *
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
using System.Runtime.CompilerServices;

namespace Server.Network
{
    public static class OutgoingMobilePackets
    {
        public const int BondedStatusPacketLength = 11;
        public const int DeathAnimationPacketLength = 13;
        public const int MobileMovingPacketLength = 17;
        public const int MobileMovingPacketCacheLength = MobileMovingPacketLength * 8 * 2; // 8 notoriety, 2 client versions
        public const int AttributeMaximum = 100;
        public const int MobileAttributePacketLength = 9;
        public const int MobileAttributesPacketLength = 17;

        public static void CreateBondedStatus(ref Span<byte> buffer, Serial serial, bool bonded)
        {
            var writer = new SpanWriter(buffer);
            writer.Write((byte)0xBF); // Packet ID
            writer.Write((ushort)11); // Length
            writer.Write((ushort)0x19); // Subpacket ID
            writer.Write((byte)0); // Command
            writer.Write(serial);
            writer.Write(bonded);
        }

        public static void SendBondedStatus(this NetState ns, Serial serial, bool bonded)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0xBF); // Packet ID
            writer.Write((ushort)11); // Length
            writer.Write((ushort)0x19); // Subpacket ID
            writer.Write((byte)0); // Command
            writer.Write(serial);
            writer.Write(bonded);

            ns.Send(ref buffer, writer.Position);
        }

        public static void CreateDeathAnimation(ref Span<byte> buffer, Serial killed, Serial corpse)
        {
            var writer = new SpanWriter(buffer);
            writer.Write((byte)0xAF); // Packet ID
            writer.Write(killed);
            writer.Write(corpse);
            writer.Write(0); // ??
        }

        public static void SendDeathAnimation(this NetState ns, Serial killed, Serial corpse)
        {
            if (ns == null)
            {
                return;
            }

            Span<byte> span = stackalloc byte[DeathAnimationPacketLength];
            CreateDeathAnimation(ref span, killed, corpse);
            ns.Send(span);
        }

        public static void CreateMobileMoving(ref Span<byte> buffer, Mobile m, int noto, bool stygianAbyss)
        {
            var loc = m.Location;
            var hue = m.SolidHueOverride >= 0 ? m.SolidHueOverride : m.Hue;

            var writer = new SpanWriter(buffer);
            writer.Write((byte)0x77); // Packet ID
            writer.Write(m.Serial);
            writer.Write((short)m.Body);
            writer.Write((short)loc.m_X);
            writer.Write((short)loc.m_Y);
            writer.Write((sbyte)loc.m_Z);
            writer.Write((byte)m.Direction);
            writer.Write((short)hue);
            writer.Write((byte)m.GetPacketFlags(stygianAbyss));
            writer.Write((byte)noto);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendMobileMoving(this NetState ns, Mobile source, Mobile target) =>
            ns.SendMobileMoving(target, Notoriety.Compute(source, target));

        public static void SendMobileMoving(this NetState ns, Mobile target, int noto)
        {
            if (ns == null)
            {
                return;
            }

            Span<byte> span = stackalloc byte[MobileMovingPacketLength];
            CreateMobileMoving(ref span, target, noto, ns.StygianAbyss);
            ns.Send(span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendMobileMovingUsingCache(this NetState ns, Span<byte> cache, Mobile source, Mobile target) =>
            ns.SendMobileMovingUsingCache(cache, target, Notoriety.Compute(source, target));

        // Requires a buffer of 16 packets, 17bytes per packet (272 bytes).
        // Requires cache to have the first byte of each packet zeroed.
        public static void SendMobileMovingUsingCache(this NetState ns, Span<byte> cache, Mobile target, int noto)
        {
            if (ns == null)
            {
                return;
            }

            var stygianAbyss = ns.StygianAbyss;
            var startIndex = (noto * 2 + (stygianAbyss ? 1 : 0)) * MobileMovingPacketLength;
            var buffer = cache.Slice(startIndex, MobileMovingPacketLength);

            // Packet not created yet
            if (buffer[0] == 0)
            {
                CreateMobileMoving(ref buffer, target, noto, stygianAbyss);
            }

            ns.Send(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteAttribute(this SpanWriter writer, int max, int cur, bool normalize = false, bool reverse = false)
        {
            if (normalize && max != 0)
            {
                if (reverse)
                {
                    writer.Write((short)(cur * AttributeMaximum / max));
                    writer.Write((short)AttributeMaximum);
                }
                else
                {
                    writer.Write((short)AttributeMaximum);
                    writer.Write((short)(cur * AttributeMaximum / max));
                }
            }
            else
            {
                if (reverse)
                {
                    writer.Write((short)cur);
                    writer.Write((short)max);
                }
                else
                {
                    writer.Write((short)max);
                    writer.Write((short)cur);
                }
            }
        }

        public static void SendMobileHits(this NetState ns, Mobile m, bool normalize = false)
        {
            if (ns == null)
            {
                return;
            }

            Span<byte> span = stackalloc byte[MobileAttributePacketLength];
            CreateMobileHits(ref span, m, normalize);
            ns.Send(span);
        }

        public static void CreateMobileHits(ref Span<byte> buffer, Mobile m, bool normalize = false)
        {
            var writer = new SpanWriter(buffer);
            writer.Write((byte)0xA1); // Packet ID
            writer.Write(m.Serial);
            writer.WriteAttribute(m.HitsMax, m.Hits, normalize);
        }

        public static void SendMobileMana(this NetState ns, Mobile m, bool normalize = false)
        {
            if (ns == null)
            {
                return;
            }

            Span<byte> span = stackalloc byte[MobileAttributePacketLength];
            CreateMobileMana(ref span, m, normalize);
            ns.Send(span);
        }

        public static void CreateMobileMana(ref Span<byte> buffer, Mobile m, bool normalize = false)
        {
            var writer = new SpanWriter(buffer);
            writer.Write((byte)0xA2); // Packet ID
            writer.Write(m.Serial);
            writer.WriteAttribute(m.ManaMax, m.Mana, normalize);
        }

        public static void SendMobileStam(this NetState ns, Mobile m, bool normalize = false)
        {
            if (ns == null)
            {
                return;
            }

            Span<byte> span = stackalloc byte[MobileAttributePacketLength];
            CreateMobileStam(ref span, m, normalize);
            ns.Send(span);
        }

        public static void CreateMobileStam(ref Span<byte> buffer, Mobile m, bool normalize = false)
        {
            var writer = new SpanWriter(buffer);
            writer.Write((byte)0xA3); // Packet ID
            writer.Write(m.Serial);
            writer.WriteAttribute(m.StamMax, m.Stam, normalize);
        }

        public static void SendMobileAttributes(this NetState ns, Mobile m, bool normalize = false)
        {
            if (ns == null)
            {
                return;
            }

            Span<byte> span = stackalloc byte[MobileAttributesPacketLength];
            CreateMobileAttributes(ref span, m, normalize);
            ns.Send(span);
        }

        public static void CreateMobileAttributes(ref Span<byte> buffer, Mobile m, bool normalize = false)
        {
            var writer = new SpanWriter(buffer);
            writer.Write((byte)0x2D); // Packet ID
            writer.Write(m.Serial);
            writer.WriteAttribute(m.HitsMax, m.Hits, normalize);
            writer.WriteAttribute(m.ManaMax, m.Mana, normalize);
            writer.WriteAttribute(m.StamMax, m.Stam, normalize);
        }
    }
}
