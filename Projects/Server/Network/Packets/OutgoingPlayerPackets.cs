/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
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

using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Server.Network;

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
    public const int DragEffectPacketLength = 26;

    public static void SendStatLockInfo(this NetState ns, Mobile m)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[12]);
        writer.Write((byte)0xBF); // Packet ID
        writer.Write((ushort)12);
        writer.Write((short)0x19);
        writer.Write((byte)2);
        writer.Write(m.Serial);
        writer.Write((byte)0);

        var lockBits = ((int)m.StrLock << 4) | ((int)m.DexLock << 2) | (int)m.IntLock;

        writer.Write((byte)lockBits);

        ns.Send(writer.Span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendChangeUpdateRange(this NetState ns, byte range) =>
        ns?.Send(stackalloc byte[] { 0xC8, range });

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendDeathStatus(this NetState ns, bool dead) =>
        ns?.Send(stackalloc byte[] { 0x2C, dead ? (byte)0 : (byte)2 });

    public static void SendDisplayProfile(this NetState ns, Serial m, string header, string body, string footer)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        header ??= "";
        body ??= "";
        footer ??= "";

        var length = 12 + header.Length + footer.Length * 2 + body.Length * 2;
        var writer = new SpanWriter(stackalloc byte[length]);
        writer.Write((byte)0xB8); // Packet ID
        writer.Write((ushort)length);
        writer.Write(m);
        writer.WriteAsciiNull(header);
        writer.WriteBigUniNull(footer);
        writer.WriteBigUniNull(body);

        ns.Send(writer.Span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendLiftReject(this NetState ns, LRReason reason) =>
        ns?.Send(stackalloc byte[] { 0x27, (byte)reason });

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendLogoutAck(this NetState ns) => ns?.Send(stackalloc byte[] { 0xD1, 0x01 });

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendWeather(this NetState ns, byte type, byte density, byte temp) =>
        ns?.Send(stackalloc byte[] { 0x65, type, density, temp });

    public static void SendServerChange(this NetState ns, Point3D p, Map map)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[16]);
        writer.Write((byte)0x76); // Packet ID
        writer.Write((short)p.X);
        writer.Write((short)p.Y);
        writer.Write((short)p.Z);
        writer.Write((byte)0);
        writer.Write((short)0);
        writer.Write((short)0);
        writer.Write((short)map.Width);
        writer.Write((short)map.Height);

        ns.Send(writer.Span);
    }

    public static void SendSkillsUpdate(this NetState ns, Skills skills)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var length = 6 + 9 * skills.Length;
        var writer = new SpanWriter(stackalloc byte[length]);
        writer.Write((byte)0x3A); // Packet ID
        writer.Write((ushort)length);
        writer.Write((byte)0x02); // type: absolute, capped

        for (var i = 0; i < skills.Length; ++i)
        {
            var s = skills[i];

            var v = s.NonRacialValue;
            var uv = Math.Clamp((int)(v * 10), 0, 0xFFFF);

            writer.Write((ushort)(s.Info.SkillID + 1));
            writer.Write((ushort)uv);
            writer.Write((ushort)s.BaseFixedPoint);
            writer.Write((byte)s.Lock);
            writer.Write((ushort)s.CapFixedPoint);
        }

        writer.Write((short)0); // terminate

        ns.Send(writer.Span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendSequence(this NetState ns, byte sequence) => ns?.Send(stackalloc byte[] { 0x7B, sequence });

    public static void SendSkillChange(this NetState ns, Skill skill)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[13]);
        writer.Write((byte)0x3A); // Packet ID
        writer.Write((ushort)13);

        var v = skill.NonRacialValue;
        var uv = Math.Clamp((int)(v * 10), 0, 0xFFFF);

        writer.Write((byte)0xDF); // type: delta, capped
        writer.Write((ushort)skill.Info.SkillID);
        writer.Write((ushort)uv);
        writer.Write((ushort)skill.BaseFixedPoint);
        writer.Write((byte)skill.Lock);
        writer.Write((ushort)skill.CapFixedPoint);

        ns.Send(writer.Span);
    }

    public static void SendLaunchBrowser(this NetState ns, string uri)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        uri ??= "";

        var length = 4 + uri.Length;
        var writer = new SpanWriter(stackalloc byte[length]);
        writer.Write((byte)0xA5); // Packet ID
        writer.Write((ushort)length);
        writer.WriteAsciiNull(uri);

        ns.Send(writer.Span);
    }

    public static void CreateDragEffect(
        Span<byte> buffer,
        Serial srcSerial, Point3D srcLocation,
        Serial trgSerial, Point3D trgLocation,
        int itemID, int hue, int amount
    )
    {
        if (buffer[0] != 0)
        {
            return;
        }

        var writer = new SpanWriter(buffer);
        writer.Write((byte)0x23); // Packet ID
        writer.Write((short)itemID);
        writer.Write((byte)0);
        writer.Write((short)hue);
        writer.Write((short)amount);
        writer.Write(srcSerial);
        writer.Write((short)srcLocation.X);
        writer.Write((short)srcLocation.Y);
        writer.Write((sbyte)srcLocation.Z);
        writer.Write(trgSerial);
        writer.Write((short)trgLocation.X);
        writer.Write((short)trgLocation.Y);
        writer.Write((sbyte)trgLocation.Z);
    }

    public static void SendDragEffect(
        this NetState ns,
        Serial srcSerial, Point3D srcLocation,
        Serial trgSerial, Point3D trgLocation,
        int itemID, int hue, int amount
    )
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        Span<byte> buffer = stackalloc byte[DragEffectPacketLength].InitializePacket();
        CreateDragEffect(buffer, srcSerial, srcLocation, trgSerial, trgLocation, itemID, hue, amount);
        ns.Send(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void SendSeasonChange(this NetState ns, byte season, bool playSound) =>
        ns?.Send(stackalloc byte[]{ 0xBC, season, *(byte*)&playSound });

    public static void SendDisplayPaperdoll(this NetState ns, Serial m, string title, bool warmode, bool canLift)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        byte flags = 0x00;

        if (warmode)
        {
            flags |= 0x01;
        }

        if (canLift)
        {
            flags |= 0x02;
        }

        var writer = new SpanWriter(stackalloc byte[66]);
        writer.Write((byte)0x88); // Packet ID
        writer.Write(m);
        writer.WriteAscii(title, 60);
        writer.Write(flags);

        ns.Send(writer.Span);
    }

    public static void SendPlayMusic(this NetState ns, MusicName music)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[3]);
        writer.Write((byte)0x6D); // Packet ID
        writer.Write((short)music);

        ns.Send(writer.Span);
    }

    public static void SendStopMusic(this NetState ns) => ns?.Send(stackalloc byte[] { 0x6D, 0x1F, 0xFF });

    public static void SendScrollMessage(this NetState ns, int type, int tip, string text)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        text ??= "";

        var length = 10 + text.Length;
        var writer = new SpanWriter(stackalloc byte[length]);
        writer.Write((byte)0xA6); // Packet ID
        writer.Write((ushort)length);
        writer.Write((byte)type);
        writer.Write(tip);
        writer.Write((ushort)text.Length);
        writer.WriteAscii(text);

        ns.Send(writer.Span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendCurrentTime(this NetState ns) => ns.SendCurrentTime(Core.Now);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendCurrentTime(this NetState ns, DateTime date) =>
        ns?.Send(stackalloc byte[] { 0x5B, (byte)date.Hour, (byte)date.Minute, (byte)date.Second });

    public static void SendPathfindMessage(this NetState ns, Point3D p)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[7]);
        writer.Write((byte)0x38); // Packet ID
        writer.Write((short)p.X);
        writer.Write((short)p.Y);
        writer.Write((short)p.Z);

        ns.Send(writer.Span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendPingAck(this NetState ns, byte ping) => ns?.Send(stackalloc byte[] { 0x73, ping });

    public static void SendDisplayHuePicker(this NetState ns, Serial huePickerSerial, int huePickerItemID)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[9]);
        writer.Write((byte)0x95); // Packet ID
        writer.Write(huePickerSerial);
        writer.Write((short)0);
        writer.Write((short)huePickerItemID);

        ns.Send(writer.Span);
    }
}
