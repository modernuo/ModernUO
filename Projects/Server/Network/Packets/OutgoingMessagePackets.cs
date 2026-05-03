/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: OutgoingMessagePackets.cs                                       *
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
using Server.Prompts;

namespace Server.Network;

[Flags]
public enum AffixType : byte
{
    Append = 0x00,
    Prepend = 0x01,
    System = 0x02
}

public static partial class OutgoingMessagePackets
{
    public static void SendMessageLocalized(
        this NetState ns,
        Serial serial, int graphic, MessageType type, int hue, int font, int number,
        ReadOnlySpan<char> name = default, ReadOnlySpan<char> args = default
    )
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var buffer = stackalloc byte[GetMaxMessageLocalizedLength(args.Length)].InitializePacket();
        var length = CreateMessageLocalized(
            buffer, serial, graphic, type, hue, font, number, name, args
        );

        ns.Send(buffer[..length]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetMaxMessageLocalizedLength(int argsCharCount) => 50 + argsCharCount * 2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetMaxMessageLocalizedLength(ReadOnlySpan<char> args) =>
        GetMaxMessageLocalizedLength(args.Length);

    public static int CreateMessageLocalized(
        Span<byte> buffer,
        Serial serial, int graphic, MessageType type, int hue, int font, int number,
        ReadOnlySpan<char> name = default, ReadOnlySpan<char> args = default
    )
    {
        if (buffer[0] != 0)
        {
            return buffer.Length;
        }

        if (hue == 0)
        {
            hue = 0x3B2;
        }

        var writer = new SpanWriter(buffer);
        writer.Write((byte)0xC1);
        writer.Seek(2, SeekOrigin.Current);
        writer.Write(serial);
        writer.Write((short)graphic);
        writer.Write((byte)type);
        writer.Write((short)hue);
        writer.Write((short)font);
        writer.Write(number);
        writer.WriteLatin1(name, 30);
        writer.WriteLittleUniNull(args);

        writer.WritePacketLength();
        return writer.Position;
    }

    public static void SendMessageLocalizedAffix(
        this NetState ns,
        Serial serial, int graphic, MessageType type, int hue, int font, int number,
        ReadOnlySpan<char> name, AffixType affixType,
        ReadOnlySpan<char> affix = default, ReadOnlySpan<char> args = default
    )
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var buffer = stackalloc byte[GetMaxMessageLocalizedAffixLength(affix.Length, args.Length)].InitializePacket();
        var length = CreateMessageLocalizedAffix(
            buffer, serial, graphic, type, hue, font, number, name, affixType, affix, args
        );

        ns.Send(buffer[..length]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetMaxMessageLocalizedAffixLength(int affixCharCount, int argsCharCount) =>
        52 + affixCharCount + argsCharCount * 2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetMaxMessageLocalizedAffixLength(ReadOnlySpan<char> affix, ReadOnlySpan<char> args) =>
        GetMaxMessageLocalizedAffixLength(affix.Length, args.Length);

    public static int CreateMessageLocalizedAffix(
        Span<byte> buffer,
        Serial serial, int graphic, MessageType type, int hue, int font, int number,
        ReadOnlySpan<char> name, AffixType affixType,
        ReadOnlySpan<char> affix = default, ReadOnlySpan<char> args = default
    )
    {
        if (buffer[0] != 0)
        {
            return buffer.Length;
        }

        if (hue == 0)
        {
            hue = 0x3B2;
        }

        var writer = new SpanWriter(buffer);
        writer.Write((byte)0xCC);
        writer.Seek(2, SeekOrigin.Current);
        writer.Write(serial);
        writer.Write((short)graphic);
        writer.Write((byte)type);
        writer.Write((short)hue);
        writer.Write((short)font);
        writer.Write(number);
        writer.Write((byte)affixType);
        writer.WriteLatin1(name, 30);
        writer.WriteLatin1Null(affix);
        writer.WriteBigUniNull(args);

        writer.WritePacketLength();
        return writer.Position;
    }

    public static void SendMessage(
        this NetState ns,
        Serial serial, int graphic, MessageType type, int hue, int font, bool ascii,
        string lang, ReadOnlySpan<char> name, ReadOnlySpan<char> text
    )
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var buffer = stackalloc byte[GetMaxMessageLength(text.Length)].InitializePacket();
        var length = CreateMessage(
            buffer,
            serial,
            graphic,
            type,
            hue,
            font,
            ascii,
            lang,
            name,
            text
        );

        ns.Send(buffer[..length]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetMaxMessageLength(int textCharCount) => 50 + textCharCount * 2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetMaxMessageLength(ReadOnlySpan<char> text) => GetMaxMessageLength(text.Length);

    public static int CreateMessage(
        Span<byte> buffer,
        Serial serial,
        int graphic,
        MessageType type,
        int hue,
        int font,
        bool ascii,
        string lang,
        ReadOnlySpan<char> name,
        ReadOnlySpan<char> text
    )
    {
        if (buffer[0] != 0)
        {
            return buffer.Length;
        }

        lang ??= "ENU";

        if (hue == 0)
        {
            hue = 0x3B2;
        }

        var writer = new SpanWriter(buffer);
        writer.Write((byte)(ascii ? 0x1C : 0xAE)); // Packet ID
        writer.Seek(2, SeekOrigin.Current);
        writer.Write(serial);
        writer.Write((short)graphic);
        writer.Write((byte)type);
        writer.Write((short)hue);
        writer.Write((short)font);
        if (ascii)
        {
            writer.WriteLatin1(name, 30);
            writer.WriteLatin1Null(text);
        }
        else
        {
            writer.WriteAscii(lang, 4);
            writer.WriteLatin1(name, 30);
            writer.WriteBigUniNull(text);
        }

        writer.WritePacketLength();
        return writer.Position;
    }

    public static void SendFollowMessage(this NetState ns, Serial s1, Serial s2)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[9]);
        writer.Write((byte)0x15); // Packet ID
        writer.Write(s1);
        writer.Write(s2);

        ns.Send(writer.Span);
    }

    public static void SendPrompt(this NetState ns, Prompt prompt)
    {
        if (ns == null || prompt == null)
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[21]);
        writer.Write((byte)0xC2); // Packet ID
        writer.Write((ushort)21);
        writer.Write(prompt.Serial);
        writer.Write(prompt.Serial);
        writer.Clear(10);

        ns.Send(writer.Span);
    }

    public static void SendHelpResponse(this NetState ns, Serial s, string text)
    {
        text = text?.Trim() ?? "";

        if (ns == null || text.Length == 0)
        {
            return;
        }

        var length = 9 + text.Length * 2;
        var writer = new SpanWriter(stackalloc byte[length]);
        writer.Write((byte)0xB7);
        writer.Write((ushort)length);
        writer.Write(s);
        writer.WriteBigUniNull(text);

        ns.Send(writer.Span);
    }
}
