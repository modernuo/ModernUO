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
        public const int MobileAnimationPacketLength = 14;
        public const int NewMobileAnimationPacketLength = 10;
        public const int MobileHealthbarPacketLength = 12;
        public const int MobileStatusCompactLength = 43;
        public const int MobileStatusMaxLength = 121;

        public static void CreateBondedStatus(Span<byte> buffer, Serial serial, bool bonded)
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

        public static void CreateDeathAnimation(Span<byte> buffer, Serial killed, Serial corpse)
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
            CreateDeathAnimation(span, killed, corpse);
            ns.Send(span);
        }

        public static void CreateMobileMoving(Span<byte> buffer, Mobile m, int noto, bool stygianAbyss)
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
            CreateMobileMoving(span, target, noto, ns.StygianAbyss);
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
                CreateMobileMoving(buffer, target, noto, stygianAbyss);
            }

            ns.Send(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteAttribute(
            this ref SpanWriter writer, int max, int cur, bool normalize = false, bool reverse = false
        )
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
            CreateMobileHits(span, m, normalize);
            ns.Send(span);
        }

        public static void CreateMobileHits(Span<byte> buffer, Mobile m, bool normalize = false)
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
            CreateMobileMana(span, m, normalize);
            ns.Send(span);
        }

        public static void CreateMobileMana(Span<byte> buffer, Mobile m, bool normalize = false)
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
            CreateMobileStam(span, m, normalize);
            ns.Send(span);
        }

        public static void CreateMobileStam(Span<byte> buffer, Mobile m, bool normalize = false)
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
            CreateMobileAttributes(span, m, normalize);
            ns.Send(span);
        }

        public static void CreateMobileAttributes(Span<byte> buffer, Mobile m, bool normalize = false)
        {
            var writer = new SpanWriter(buffer);
            writer.Write((byte)0x2D); // Packet ID
            writer.Write(m.Serial);

            writer.WriteAttribute(m.HitsMax, m.Hits, normalize);
            writer.WriteAttribute(m.ManaMax, m.Mana, normalize);
            writer.WriteAttribute(m.StamMax, m.Stam, normalize);
        }

        public static void SendMobileName(this NetState ns, Mobile m)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0x98); // Packet ID
            writer.Write((ushort)37);
            writer.Write(m.Serial);
            writer.WriteAscii(m.Name ?? "", 29);
            writer.Write((byte)0); // Null terminator

            ns.Send(ref buffer, writer.Position);
        }

        public static void CreateMobileAnimation(
            Span<byte> buffer,
            Serial mobile, int action, int frameCount, int repeatCount, bool forward, bool repeat, int delay
        )
        {
            var writer = new SpanWriter(buffer);
            writer.Write((byte)0x6E); // Packet ID
            writer.Write(mobile);
            writer.Write((short)action);
            writer.Write((short)frameCount);
            writer.Write((short)repeatCount);
            writer.Write(!forward); // protocol has really "reverse" but I find this more intuitive
            writer.Write(repeat);
            writer.Write((byte)delay);
        }

        public static void SendMobileAnimation(
            this NetState ns,
            Serial mobile, int action, int frameCount, int repeatCount, bool forward, bool repeat, int delay
        )
        {
            if (ns == null)
            {
                return;
            }

            Span<byte> span = stackalloc byte[MobileAnimationPacketLength];
            CreateMobileAnimation(span, mobile, action, frameCount, repeatCount, forward, repeat, delay);
            ns.Send(span);
        }

        public static void CreateNewMobileAnimation(
            Span<byte> buffer,
            Serial mobile, int action, int frameCount, int delay
        )
        {
            var writer = new SpanWriter(buffer);
            writer.Write((byte)0xE2); // Packet ID
            writer.Write(mobile);
            writer.Write((short)action);
            writer.Write((short)frameCount);
            writer.Write((byte)delay);
        }

        public static void SendNewMobileAnimation(this NetState ns, Serial mobile, int action, int frameCount, int delay)
        {
            if (ns == null)
            {
                return;
            }

            Span<byte> span = stackalloc byte[NewMobileAnimationPacketLength];
            CreateNewMobileAnimation(span, mobile, action, frameCount, delay);
            ns.Send(span);
        }

        public static void SendMobileHealthbar(this NetState ns, Mobile m, Healthbar healthbar)
        {
            if (ns == null)
            {
                return;
            }

            Span<byte> span = stackalloc byte[MobileHealthbarPacketLength];
            CreateMobileHealthbar(span, m, healthbar);
            ns.Send(span);
        }

        public static void CreateMobileHealthbar(Span<byte> buffer, Mobile m, Healthbar healthbar)
        {
            switch (healthbar)
            {
                case Healthbar.Poison:
                    {
                        CreateMobileHealthbar(buffer, m.Serial, Healthbar.Poison, m.Poison?.Level + 1 ?? 0);
                        break;
                    }
                case Healthbar.Yellow:
                    {
                        CreateMobileHealthbar(buffer, m.Serial, Healthbar.Yellow, m.Blessed || m.YellowHealthbar ? 1 : 0);
                        break;
                    }
                default:
                    {
                        Console.WriteLine("Packets: Invalid Healthbar {0} in {1}", healthbar, nameof(CreateMobileHealthbar));
                        CreateMobileHealthbar(buffer, m.Serial, Healthbar.Normal, 0);
                        break;
                    }
            }
        }

        public static void CreateMobileHealthbar(Span<byte> buffer, Serial serial, Healthbar healthbar, int level)
        {
            var writer = new SpanWriter(buffer);
            writer.Write((byte)0x17); // Packet ID
            writer.Write((ushort)12);
            writer.Write(serial);
            writer.Write((short)1); // Show bar
            writer.Write((short)healthbar);
            writer.Write((byte)level); // 0 is off for that bar type
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CreateMobileStatusCompact(Span<byte> buffer, Mobile m, bool canBeRenamed) =>
            CreateMobileStatus(buffer, m, m, 0, canBeRenamed);

        public static void SendMobileStatusCompact(this NetState ns, Mobile m, bool canBeRenamed)
        {
            if (ns == null)
            {
                return;
            }

            Span<byte> buffer = stackalloc byte[MobileStatusCompactLength];
            int length = CreateMobileStatusCompact(buffer, m, canBeRenamed);
            buffer = buffer.Slice(0, length);

            ns.Send(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendMobileStatus(this NetState ns, Mobile m) => ns.SendMobileStatus(m, m);

        public static void SendMobileStatus(this NetState ns, Mobile beholder, Mobile beheld)
        {
            if (ns == null)
            {
                return;
            }

            Span<byte> buffer = stackalloc byte[MobileStatusMaxLength];
            int version;

            if (beholder != beheld)
            {
                version = 0;
            }
            else if (Core.HS && ns.ExtendedStatus)
            {
                version = 6;
            }
            else if (Core.ML && ns.SupportsExpansion(Expansion.ML))
            {
                version = 5;
            }
            else
            {
                version = Core.AOS ? 4 : 3;
            }

            var length = CreateMobileStatus(buffer, beholder, beheld, version, beheld.CanBeRenamedBy(beholder));
            ns.Send(buffer.Slice(0, length));
        }

        public static int CreateMobileStatus(
            Span<byte> buffer, Mobile beholder, Mobile beheld, int version, bool canBeRenamed
        )
        {
            var name = beheld.Name ?? "";

            var writer = new SpanWriter(buffer);
            writer.Write((byte)0x11); // Packet ID
            writer.Write((ushort)43);

            writer.Write(beheld.Serial);

            writer.WriteAscii(name, 30);

            var maxHits = beheld.HitsMax;
            var curHits = beheld.Hits;

            writer.WriteAttribute(maxHits, curHits, beholder != beheld, true);

            writer.Write(canBeRenamed);

            writer.Write((byte)version);

            if (version <= 0)
            {
                return writer.Position;
            }

            writer.Write(beheld.Female);

            writer.Write((short)beheld.Str);
            writer.Write((short)beheld.Dex);
            writer.Write((short)beheld.Int);

            writer.Write((short)beheld.Stam);
            writer.Write((short)beheld.StamMax);
            writer.Write((short)beheld.Mana);
            writer.Write((short)beheld.ManaMax);

            writer.Write(beheld.TotalGold);
            writer.Write((short)(Core.AOS ? beheld.PhysicalResistance : (int)(beheld.ArmorRating + 0.5)));
            writer.Write((short)(Mobile.BodyWeight + beheld.TotalWeight));

            if (version >= 5)
            {
                writer.Write((short)beheld.MaxWeight);
                writer.Write((byte)(beheld.Race.RaceID + 1)); // Would be 0x00 if it's a non-ML enabled account but...
            }

            writer.Write((short)beheld.StatCap);

            writer.Write((byte)beheld.Followers);
            writer.Write((byte)beheld.FollowersMax);

            if (version >= 4)
            {
                writer.Write((short)beheld.FireResistance);   // Fire
                writer.Write((short)beheld.ColdResistance);   // Cold
                writer.Write((short)beheld.PoisonResistance); // Poison
                writer.Write((short)beheld.EnergyResistance); // Energy
                writer.Write((short)beheld.Luck);             // Luck

                var weapon = beheld.Weapon;

                if (weapon != null)
                {
                    weapon.GetStatusDamage(beheld, out var min, out var max);
                    writer.Write((short)min); // Damage min
                    writer.Write((short)max); // Damage max
                }
                else
                {
                    writer.Write((short)0); // Damage min
                    writer.Write((short)0); // Damage max
                }

                writer.Write(beheld.TithingPoints);
            }

            if (version >= 6)
            {
                for (var i = 0; i < 15; ++i)
                {
                    writer.Write((short)beheld.GetAOSStatus(i));
                }
            }


            return writer.Position;
        }
    }
}
