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
        public const int MobileStatsPacketLength = 9;
        public const int MobileAttributesPacketLength = 17;
        public const int MobileAnimationLength = 14;
        public const int MobileStatusCompactLength = 43;
        public const int MobileStatusLength = 121; // Max

        public static int StatMaximum { get; set; } = 25;
        public static bool UseStatNormalizer { get; set; } = true;

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
            if (ns == null)
            {
                return;
            }

            Span<byte> buffer = stackalloc byte[BondedStatusPacketLength];
            CreateBondedStatus(ref buffer, serial, bonded);

            ns.Send(buffer);
        }

        public static void CreateDeathAnimation(ref Span<byte> buffer, Serial killed, Serial corpse)
        {
            var writer = new SpanWriter(buffer);
            writer.Write((byte)0xAF); // Packet ID
            writer.Write(killed);
            writer.Write(corpse);
            writer.Write(0); // ??
        }

        public static void CreateMobileMoving(ref Span<byte> buffer, Mobile m, int noto, bool stygianAbyss)
        {
            var writer = new SpanWriter(buffer);

            var loc = m.Location;

            var hue = m.Hue;

            if (m.SolidHueOverride >= 0)
            {
                hue = m.SolidHueOverride;
            }

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
        private static void WriteAttribute(this ref SpanWriter writer, int max, int cur, bool normalize)
        {
            if (UseStatNormalizer && max != 0 && normalize)
            {
                writer.Write((short)StatMaximum);
                writer.Write((short)(cur * StatMaximum / max));
            }
            else
            {
                writer.Write((short)max);
                writer.Write((short)cur);
            }
        }

        public static void CreateMobileHits(ref Span<byte> buffer, Mobile m, bool normalize = false)
        {
            var writer = new SpanWriter(buffer);
            writer.Write((byte)0xA1); // Packet ID
            writer.Write(m.Serial);
            writer.WriteAttribute(m.HitsMax, m.Hits, normalize);
        }

        public static void CreateMobileMana(ref Span<byte> buffer, Mobile m, bool normalize = false)
        {
            var writer = new SpanWriter(buffer);
            writer.Write((byte)0xA2); // Packet ID
            writer.Write(m.Serial);
            writer.WriteAttribute(m.ManaMax, m.Mana, normalize);
        }

        public static void CreateMobileStam(ref Span<byte> buffer, Mobile m, bool normalize = false)
        {
            var writer = new SpanWriter(buffer);
            writer.Write((byte)0xA3); // Packet ID
            writer.Write(m.Serial);
            writer.WriteAttribute(m.StamMax, m.Stam, normalize);
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

        public static void SendMobileName(this NetState ns, Serial s, string name)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            name ??= "";

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0x98); // Packet ID
            writer.Write((ushort)37);
            writer.Write(s);
            // This can be dynamically sized, but we cap it at 29 + terminator
            writer.WriteAscii(name, 29);
            writer.Write((byte)0);
        }

        public static void CreateMobileAnimation(
            ref Span<byte> buffer,
            Serial mobile, int action, int frameCount, int repeatCount, bool forward, bool repeat, int delay
        )
        {
            var writer = new SpanWriter(buffer);
            writer.Write((byte)0x6E); // Packet ID
            writer.Write(mobile);
            writer.Write((short)action);
            writer.Write((short)frameCount);
            writer.Write((short)repeatCount);
            writer.Write(!forward);
            writer.Write(repeat);
            writer.Write((byte)delay);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CreateMobileStatusCompact(ref Span<byte> buffer, Mobile m, bool canBeRenamed) =>
            CreateMobileStatus(ref buffer, m, m, 0, canBeRenamed);

        public static void SendMobileStatusCompact(this NetState ns, Mobile m, bool canBeRenamed)
        {
            if (ns == null)
            {
                return;
            }

            Span<byte> buffer = stackalloc byte[MobileStatusCompactLength];
            CreateMobileStatusCompact(ref buffer, m, canBeRenamed);

            ns.Send(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendMobileStatusExtended(this NetState ns, Mobile m) => ns.SendMobileStatus(m, m);

        public static void SendMobileStatus(this NetState ns, Mobile beholder, Mobile beheld)
        {
            if (ns == null)
            {
                return;
            }

            Span<byte> buffer = stackalloc byte[MobileStatusLength];
            int version;

            if (beholder != beheld)
            {
                version = 0;
            }
            else if (Core.HS && ns.ExtendedStatus)
            {
                version = 6;
            }
            else if (Core.ML && ns?.SupportsExpansion(Expansion.ML) == true)
            {
                version = 5;
            }
            else
            {
                version = Core.AOS ? 4 : 3;
            }

            var length = CreateMobileStatus(ref buffer, beholder, beheld, version, beheld.CanBeRenamedBy(beholder));
            ns.Send(buffer.Slice(0, length));
        }

        public static int CreateMobileStatus(ref Span<byte> buffer, Mobile beholder, Mobile beheld, int version, bool canBeRenamed)
        {
            var name = beheld.Name ?? "";

            var writer = new SpanWriter(buffer);
            writer.Write((byte)0x11); // Packet ID
            writer.Write((ushort)43);

            writer.Write(beheld.Serial);

            writer.WriteAscii(name, 30);

            var maxHits = beheld.HitsMax;
            var curHits = beheld.Hits;

            if (beholder != beheld && UseStatNormalizer && maxHits != 0)
            {
                writer.Write((short)(curHits * StatMaximum / maxHits));
                writer.Write((short)StatMaximum);
            }
            else
            {
                writer.Write((short)curHits);
                writer.Write((short)maxHits);
            }

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
