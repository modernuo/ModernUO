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
using System.IO;
using System.Runtime.CompilerServices;

namespace Server.Network;

public static class OutgoingMobilePackets
{
    public const int BondedStatusPacketLength = 11;
    public const int DeathAnimationPacketLength = 13;
    public const int MobileMovingPacketLength = 17;
    public const int MobileMovingPacketCacheHeight = 7 * 2; // 7 notoriety, 2 client versions
    public const int MobileMovingPacketCacheByteLength = MobileMovingPacketLength * MobileMovingPacketCacheHeight;
    public const int AttributeMaximum = 100;
    public const int MobileAttributePacketLength = 9;
    public const int MobileAttributesPacketLength = 17;
    public const int MobileAnimationPacketLength = 14;
    public const int NewMobileAnimationPacketLength = 10;
    public const int MobileHealthbarPacketLength = 12;
    public const int MobileStatusCompactLength = 43;
    public const int MobileStatusLength = 70;
    public const int MobileStatusAOSLength = 88;
    public const int MobileStatusMLLength = 91;
    public const int MobileStatusHSLength = 121;

    public static bool ExtendedStatus { get; private set; } = true;

    public static void Configure()
    {
        ExtendedStatus = ServerConfiguration.GetSetting("client.showExtendedStatus", true);
    }

    public static void CreateBondedStatus(Span<byte> buffer, Serial serial, bool bonded)
    {
        if (buffer[0] != 0)
        {
            return;
        }

        var writer = new SpanWriter(buffer);
        writer.Write((byte)0xBF);   // Packet ID
        writer.Write((ushort)11);   // Length
        writer.Write((ushort)0x19); // Subpacket ID
        writer.Write((byte)0);      // Command
        writer.Write(serial);
        writer.Write(bonded);
    }

    public static void SendBondedStatus(this NetState ns, Serial serial, bool bonded)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[11]);
        writer.Write((byte)0xBF);   // Packet ID
        writer.Write((ushort)11);   // Length
        writer.Write((ushort)0x19); // Subpacket ID
        writer.Write((byte)0);      // Command
        writer.Write(serial);
        writer.Write(bonded);

        ns.Send(writer.Span);
    }

    public static void CreateDeathAnimation(Span<byte> buffer, Serial killed, Serial corpse)
    {
        if (buffer[0] != 0)
        {
            return;
        }

        var writer = new SpanWriter(buffer);
        writer.Write((byte)0xAF); // Packet ID
        writer.Write(killed);
        writer.Write(corpse);
        writer.Write(0); // ??
    }

    public static void SendDeathAnimation(this NetState ns, Serial killed, Serial corpse)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        Span<byte> span = stackalloc byte[DeathAnimationPacketLength];
        CreateDeathAnimation(span, killed, corpse);
        ns.Send(span);
    }

    public static void CreateMobileMoving(Span<byte> buffer, Mobile m, int noto, bool stygianAbyss)
    {
        if (buffer[0] != 0)
        {
            return;
        }

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
        if (ns.CannotSendPackets())
        {
            return;
        }

        Span<byte> buffer = stackalloc byte[MobileMovingPacketLength].InitializePacket();
        CreateMobileMoving(buffer, target, noto, ns.StygianAbyss);
        ns.Send(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendMobileMovingUsingCache(this NetState ns, Span<byte> cache, Mobile source, Mobile target) =>
        ns.SendMobileMovingUsingCache(cache, target, Notoriety.Compute(source, target));

    // Requires a buffer of 14 packets, 17 bytes per packet (238 bytes).
    // Requires cache to have the first byte of each packet initially zeroed.
    public static void SendMobileMovingUsingCache(this NetState ns, Span<byte> cache, Mobile target, int noto)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var stygianAbyss = ns.StygianAbyss;
        // Indexes 0-6 for pre-SA, and 7-13 for SA
        var row = noto + (stygianAbyss ? 6 : -1);
        var buffer = cache.Slice(row * MobileMovingPacketLength, MobileMovingPacketLength);
        CreateMobileMoving(buffer, target, noto, stygianAbyss);

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
        if (ns.CannotSendPackets())
        {
            return;
        }

        Span<byte> span = stackalloc byte[MobileAttributePacketLength];
        CreateMobileHits(span, m, normalize);
        ns.Send(span);
    }

    public static void CreateMobileHits(Span<byte> buffer, Mobile m, bool normalize = false)
    {
        if (buffer[0] != 0)
        {
            return;
        }

        var writer = new SpanWriter(buffer);
        writer.Write((byte)0xA1); // Packet ID
        writer.Write(m.Serial);
        writer.WriteAttribute(m.HitsMax, m.Hits, normalize);
    }

    public static void SendMobileMana(this NetState ns, Mobile m, bool normalize = false)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        Span<byte> span = stackalloc byte[MobileAttributePacketLength];
        CreateMobileMana(span, m, normalize);
        ns.Send(span);
    }

    public static void CreateMobileMana(Span<byte> buffer, Mobile m, bool normalize = false)
    {
        if (buffer[0] != 0)
        {
            return;
        }

        var writer = new SpanWriter(buffer);
        writer.Write((byte)0xA2); // Packet ID
        writer.Write(m.Serial);
        writer.WriteAttribute(m.ManaMax, m.Mana, normalize);
    }

    public static void SendMobileStam(this NetState ns, Mobile m, bool normalize = false)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        Span<byte> span = stackalloc byte[MobileAttributePacketLength];
        CreateMobileStam(span, m, normalize);
        ns.Send(span);
    }

    public static void CreateMobileStam(Span<byte> buffer, Mobile m, bool normalize = false)
    {
        if (buffer[0] != 0)
        {
            return;
        }

        var writer = new SpanWriter(buffer);
        writer.Write((byte)0xA3); // Packet ID
        writer.Write(m.Serial);
        writer.WriteAttribute(m.StamMax, m.Stam, normalize);
    }

    public static void SendMobileAttributes(this NetState ns, Mobile m, bool normalize = false)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        Span<byte> span = stackalloc byte[MobileAttributesPacketLength];
        CreateMobileAttributes(span, m, normalize);
        ns.Send(span);
    }

    public static void CreateMobileAttributes(Span<byte> buffer, Mobile m, bool normalize = false)
    {
        if (buffer[0] != 0)
        {
            return;
        }

        var writer = new SpanWriter(buffer);
        writer.Write((byte)0x2D); // Packet ID
        writer.Write(m.Serial);

        writer.WriteAttribute(m.HitsMax, m.Hits, normalize);
        writer.WriteAttribute(m.ManaMax, m.Mana, normalize);
        writer.WriteAttribute(m.StamMax, m.Stam, normalize);
    }

    public static void SendMobileName(this NetState ns, Mobile m)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[37]);
        writer.Write((byte)0x98); // Packet ID
        writer.Write((ushort)37);
        writer.Write(m.Serial);
        writer.WriteAscii(m.Name ?? "", 29);
        writer.Write((byte)0); // Null terminator

        ns.Send(writer.Span);
    }

    public static void CreateMobileAnimation(
        Span<byte> buffer,
        Serial mobile, int action, int frameCount, int repeatCount, bool forward, bool repeat, int delay
    )
    {
        if (buffer[0] != 0)
        {
            return;
        }

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
        if (ns.CannotSendPackets())
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
        if (ns.CannotSendPackets())
        {
            return;
        }

        Span<byte> span = stackalloc byte[NewMobileAnimationPacketLength];
        CreateNewMobileAnimation(span, mobile, action, frameCount, delay);
        ns.Send(span);
    }

    public static void SendMobileHealthbar(this NetState ns, Mobile m, Healthbar healthbar)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        Span<byte> span = stackalloc byte[MobileHealthbarPacketLength];
        CreateMobileHealthbar(span, m, healthbar);
        ns.Send(span);
    }

    public static void CreateMobileHealthbar(Span<byte> buffer, Mobile m, Healthbar healthbar)
    {
        if (buffer[0] != 0)
        {
            return;
        }

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
    public static void CreateMobileStatusCompact(Span<byte> buffer, Mobile m, bool canBeRenamed) =>
        CreateMobileStatus(buffer, m, 0, canBeRenamed);

    public static void SendMobileStatusCompact(this NetState ns, Mobile m, bool canBeRenamed)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        Span<byte> buffer = stackalloc byte[MobileStatusCompactLength];
        CreateMobileStatusCompact(buffer, m, canBeRenamed);

        ns.Send(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendMobileStatus(this NetState ns, Mobile m) => ns.SendMobileStatus(m, m);

    public static void SendMobileStatus(this NetState ns, Mobile beholder, Mobile beheld)
    {
        if (ns.CannotSendPackets() || beheld == null)
        {
            return;
        }

        int version;
        int length;

        if (beholder != beheld)
        {
            version = 0;
            length = MobileStatusCompactLength;
        }
        else if (ExtendedStatus && ns.ExtendedStatus)
        {
            version = 6;
            length = MobileStatusHSLength;
        }
        else if (Core.ML && ns.SupportsExpansion(Expansion.ML))
        {
            version = 5;
            length = MobileStatusMLLength;
        }
        else if (Core.AOS)
        {
            version = 4;
            length = MobileStatusAOSLength;
        }
        else
        {
            version = 3;
            length = MobileStatusLength;
        }

        Span<byte> buffer = stackalloc byte[length];
        CreateMobileStatus(buffer, beheld, version, beheld.CanBeRenamedBy(beholder));
        ns.Send(buffer);
    }

    public static void CreateMobileStatus(Span<byte> buffer, Mobile beheld, int version, bool canBeRenamed)
    {
        if (buffer[0] != 0)
        {
            return;
        }

        var name = beheld.Name ?? "";

        var writer = new SpanWriter(buffer);
        writer.Write((byte)0x11); // Packet ID
        writer.Seek(2, SeekOrigin.Current);
        writer.Write(beheld.Serial);
        writer.WriteAscii(name, 30);
        writer.WriteAttribute(beheld.HitsMax, beheld.Hits, version == 0, true);
        writer.Write(canBeRenamed);
        writer.Write((byte)version);

        if (version <= 0)
        {
            writer.WritePacketLength();
            return;
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
            writer.Write((byte)(beheld.Race?.RaceID + 1 ?? 0)); // Would be 0x00 if it's a non-ML enabled account but...
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

            int min = 0, max = 0;
            weapon?.GetStatusDamage(beheld, out min, out max);
            writer.Write((short)min); // Damage min
            writer.Write((short)max); // Damage max

            writer.Write(beheld.TithingPoints);
        }

        if (version >= 6)
        {
            for (var i = 0; i < 15; ++i)
            {
                writer.Write((short)beheld.GetAOSStatus(i));
            }
        }

        writer.WritePacketLength();
    }

    public static void SendMobileUpdate(this NetState ns, Mobile m)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[19]);
        writer.Write((byte)0x20); // Packet ID
        writer.Write(m.Serial);
        writer.Write((short)m.Body);
        writer.Write((byte)0);
        writer.Write((short)(m.SolidHueOverride >= 0 ? m.SolidHueOverride : m.Hue));
        writer.Write((byte)m.GetPacketFlags(ns.StygianAbyss));
        writer.Write((short)m.X);
        writer.Write((short)m.Y);
        writer.Write((short)0);
        writer.Write((byte)m.Direction);
        writer.Write((sbyte)m.Z);

        ns.Send(writer.Span);
    }

    public static void SendMobileIncoming(this NetState ns, Mobile beholder, Mobile beheld)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        Span<bool> layers = stackalloc bool[256];
#if NO_LOCAL_INIT
            layers.Clear();
#endif

        var eq = beheld.Items;
        var maxLength = 23 + (eq.Count + 2) * 9;
        var writer = new SpanWriter(stackalloc byte[maxLength]);
        writer.Write((byte)0x78); // Packet ID
        writer.Seek(2, SeekOrigin.Current);

        var sa = ns.StygianAbyss;
        var newPacket = ns.NewMobileIncoming;
        var itemIdMask = newPacket ? 0xFFFF : 0x7FFF;

        var hue = beheld.SolidHueOverride >= 0 ? beheld.SolidHueOverride : beheld.Hue;

        writer.Write(beheld.Serial);
        writer.Write((short)beheld.Body);
        writer.Write((short)beheld.X);
        writer.Write((short)beheld.Y);
        writer.Write((sbyte)beheld.Z);
        writer.Write((byte)beheld.Direction);
        writer.Write((short)hue);
        writer.Write((byte)beheld.GetPacketFlags(sa));
        writer.Write((byte)Notoriety.Compute(beholder, beheld));

        for (var i = 0; i < eq.Count; ++i)
        {
            var item = eq[i];
            var layer = (byte)item.Layer;

            if (item.Deleted || !beholder.CanSee(item) || layers[layer])
            {
                continue;
            }

            layers[layer] = true;
            hue = beheld.SolidHueOverride >= 0 ? beheld.SolidHueOverride : item.Hue;

            var itemID = item.ItemID & itemIdMask;
            var writeHue = newPacket || hue != 0;

            if (!newPacket && writeHue)
            {
                itemID |= 0x8000;
            }

            writer.Write(item.Serial);
            writer.Write((ushort)itemID);
            writer.Write(layer);

            if (writeHue)
            {
                writer.Write((short)hue);
            }
        }

        if (beheld.HairItemID > 0 && !layers[(int)Layer.Hair])
        {
            layers[(int)Layer.Hair] = true;
            hue = beheld.SolidHueOverride >= 0 ? beheld.SolidHueOverride : beheld.HairHue;

            var itemID = beheld.HairItemID & itemIdMask;
            var writeHue = newPacket || hue != 0;

            if (!newPacket && writeHue)
            {
                itemID |= 0x8000;
            }

            writer.Write(HairInfo.FakeSerial(beheld.Serial));
            writer.Write((ushort)itemID);
            writer.Write((byte)Layer.Hair);

            if (writeHue)
            {
                writer.Write((short)hue);
            }
        }

        if (beheld.FacialHairItemID > 0 && !layers[(int)Layer.FacialHair])
        {
            layers[(int)Layer.FacialHair] = true;
            hue = beheld.SolidHueOverride >= 0 ? beheld.SolidHueOverride : beheld.FacialHairHue;

            var itemID = beheld.FacialHairItemID & itemIdMask;
            var writeHue = newPacket || hue != 0;

            if (!newPacket && writeHue)
            {
                itemID |= 0x8000;
            }

            writer.Write(FacialHairInfo.FakeSerial(beheld.Serial));
            writer.Write((ushort)itemID);
            writer.Write((byte)Layer.FacialHair);

            if (writeHue)
            {
                writer.Write((short)hue);
            }
        }

        writer.Write(0); // terminate

        writer.WritePacketLength();
        ns.Send(writer.Span);
    }
}
