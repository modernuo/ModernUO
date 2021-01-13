/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: BulletinBoardPackets.cs                                         *
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
using Server.Items;

namespace Server.Network
{
    public static class BulletinBoardPackets
    {
        public static void SendBBDisplayBoard(this NetState ns, BaseBulletinBoard board)
        {
            if (ns == null)
            {
                return;
            }

            var writer = new SpanWriter(stackalloc byte[38]);
            writer.Write((byte)0x71); // Packet ID
            writer.Write((ushort)38);
            writer.Write((byte)0x00); // Command
            writer.Write(board.Serial);

            var text = board.BoardName ?? "";

            var textChars = text.AsSpan(0, Math.Min(text.Length, 30));
            Span<byte> textBuffer = stackalloc byte[Utility.UTF8.GetMaxByteCount(textChars.Length)]; // Max 30 * 3 (90 bytes)

            // We are ok with the string being cut-off mid character. The alternative is very slow.
            // The board name should be regulated by staff, who can test proper strings that will look correct.
            var byteLength = Math.Min(29, Utility.UTF8.GetBytes(textChars, textBuffer));
            writer.Write(textBuffer.Slice(0, byteLength));
            writer.Clear(30 - byteLength); // terminator

            ns.Send(writer.Span);
        }

        public static void SendBBMessage(this NetState ns, BaseBulletinBoard board, BulletinMessage msg, bool content = false)
        {
            if (ns == null)
            {
                return;
            }

            var maxLength = 22;
            var poster = msg.PostedName ?? "";
            maxLength += Utility.UTF8.GetMaxByteCount(Math.Min(255, poster.Length));

            var subject = msg.Subject ?? "";
            maxLength += Utility.UTF8.GetMaxByteCount(Math.Min(255, subject.Length));

            var time = msg.GetTimeAsString() ?? "";
            maxLength += Utility.UTF8.GetMaxByteCount(Math.Min(255, time.Length));

            if (content)
            {
                maxLength += 6 + msg.PostedEquip.Length * 4;
                for (int i = 0; i < msg.Lines.Length; i++)
                {
                    var length = msg.Lines[i].Length;
                    maxLength += Utility.UTF8.GetMaxByteCount(Math.Min(255, length));
                }
            }

            Span<byte> textBuffer = stackalloc byte[Utility.UTF8.GetMaxByteCount(255)]; // 3 * 255 bytes

            var writer = maxLength > 81920 ? new SpanWriter(maxLength) : new SpanWriter(stackalloc byte[maxLength]);
            writer.Write((byte)0x71); // Packet ID
            writer.Seek(2, SeekOrigin.Current);
            writer.Write((byte)(content ? 0x02 : 0x01)); // Command
            writer.Write(board.Serial);
            writer.Write(msg.Serial);
            writer.Write(msg.Thread?.Serial ?? Serial.Zero);

            writer.WriteString(poster, textBuffer);
            writer.WriteString(subject, textBuffer);
            writer.WriteString(time, textBuffer);

            if (content)
            {
                writer.Write((short)msg.PostedBody);
                writer.Write((short)msg.PostedHue);

                var length = Math.Min(255, msg.PostedEquip.Length);
                writer.Write((byte)length);
                for (var i = 0; i < length; i++)
                {
                    var eq = msg.PostedEquip[i];
                    writer.Write((short)eq.itemID);
                    writer.Write((short)eq.hue);
                }

                length = msg.Lines.Length;
                writer.Write((byte)length);
                for (var i = 0; i < length; i++)
                {
                    writer.WriteString(msg.Lines[i], textBuffer, true);
                }
            }

            writer.WritePacketLength();
            ns.Send(writer.Span);

            writer.Dispose();
        }

        private static void WriteString(this ref SpanWriter writer, string text, Span<byte> buffer, bool pad = false)
        {
            // TODO: Make this safer since we will be slicing mid-character if the string is >= 255 bytes
            var length = Math.Min(255, Utility.UTF8.GetBytes(text, buffer));
            writer.Write((byte)length);
            writer.Write(buffer.SliceToLength(length - (pad ? 2 : 1)));

            if (pad)
            {
                writer.Write((ushort)0); // Compensating for an old client bug
            }
            else
            {
                writer.Write((byte)0); // Terminator
            }
        }
    }
}
