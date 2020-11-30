/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
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

namespace Server.Network
{
    [Flags]
    public enum AffixType : byte
    {
        Append = 0x00,
        Prepend = 0x01,
        System = 0x02
    }

    public static class OutgoingMessagePackets
    {
        public static void SendMessageLocalized(
            this NetState ns,
            Serial serial, int graphic, MessageType type, int hue, int font, int number, string name = "", string args = ""
        )
        {
            if (ns == null)
            {
                return;
            }

            Span<byte> buffer = stackalloc byte[GetMaxMessageLocalizedLength(args)];
            var length = CreateMessageLocalized(
                ref buffer,
                serial, graphic, type, hue, font, number, name, args
            );

            ns.Send(buffer.Slice(0, length));
        }

        public static int GetMaxMessageLocalizedLength(string args) => 50 + (args?.Length ?? 0) * 2;

        public static int CreateMessageLocalized(
            ref Span<byte> buffer,
            Serial serial, int graphic, MessageType type, int hue, int font, int number, string name = "", string args = ""
        )
        {
            name ??= "";
            args ??= "";

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
            writer.WriteAscii(name, 30);
            writer.WriteLittleUniNull(args);

            writer.WritePacketLength();

            return writer.Position;
        }

        public static void SendMessageLocalizedAffix(
            this NetState ns,
            Serial serial, int graphic, MessageType type, int hue, int font, int number, string name,
            AffixType affixType, string affix = "", string args = ""
        )
        {
            if (ns == null)
            {
                return;
            }

            Span<byte> buffer = stackalloc byte[GetMaxMessageLocalizedAffixLength(affix, args)];
            var length = CreateMessageLocalizedAffix(
                ref buffer,
                serial, graphic, type, hue, font, number, name, affixType, affix, args
            );

            ns.Send(buffer.Slice(0, length));
        }

        public static int GetMaxMessageLocalizedAffixLength(string affix, string args) =>
            52 + (affix?.Length ?? 0) + (args?.Length ?? 0) * 2;

        public static int CreateMessageLocalizedAffix(
            ref Span<byte> buffer,
            Serial serial, int graphic, MessageType type, int hue, int font, int number, string name,
            AffixType affixType, string affix = "", string args = ""
        )
        {
            name ??= "";
            affix ??= "";
            args ??= "";

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
            writer.WriteAscii(name, 30);
            writer.WriteAsciiNull(affix);
            writer.WriteBigUniNull(args);

            writer.WritePacketLength();

            return writer.Position;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SendMessage(
            this NetState ns,
            Serial serial, int graphic, MessageType type, int hue, int font, bool ascii, string lang, string name, string text
        )
        {
            if (ns == null)
            {
                return;
            }

            Span<byte> buffer = stackalloc byte[GetMaxMessageLength(text)];
            var length = CreateMessage(
                ref buffer,
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

            ns.Send(buffer.Slice(0, length));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetMaxMessageLength(string text) => 50 + (text?.Length ?? 0) * 2;

        public static int CreateMessage(
            ref Span<byte> buffer,
            Serial serial,
            int graphic,
            MessageType type,
            int hue,
            int font,
            bool ascii,
            string lang,
            string name,
            string text
        )
        {
            name ??= "";
            text ??= "";
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
                writer.WriteAscii(name, 30);
                writer.WriteAsciiNull(text);
            }
            else
            {
                writer.WriteAscii(lang, 4);
                writer.WriteAscii(name, 30);
                writer.WriteBigUniNull(text);
            }

            writer.WritePacketLength();

            return writer.Position;
        }

        public static void SendFollowMessage(this NetState ns, Serial s1, Serial s2)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0x15); // Packet ID
            writer.Write(s1);
            writer.Write(s2);

            ns.Send(ref buffer, 9);
        }
    }
}
