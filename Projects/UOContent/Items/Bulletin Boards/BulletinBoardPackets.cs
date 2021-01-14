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
using System.Runtime.CompilerServices;
using Server.Items;
using Server.Text;

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
            Span<byte> textBuffer = stackalloc byte[TextEncoding.UTF8.GetMaxByteCount(textChars.Length)]; // Max 30 * 3 (90 bytes)

            // We are ok with the string being cut-off mid character. The alternative is very slow.
            var byteLength = Math.Min(29, TextEncoding.UTF8.GetBytes(textChars, textBuffer));
            writer.Write(textBuffer.SliceToLength(byteLength));
            writer.Clear(30 - byteLength); // terminator

            ns.Send(writer.Span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void UpdateLengthCounters(this string text, ref int maxLength, ref int longestTextLine, bool pad = false)
        {
            var line = Math.Min(255, text.Length);
            var byteCount = TextEncoding.UTF8.GetMaxByteCount(line) + (pad ? 3 : 2); // 1 + length + terminator
            maxLength += byteCount;
            longestTextLine = Math.Max(byteCount, longestTextLine);
        }

        public static void SendBBMessage(this NetState ns, BaseBulletinBoard board, BulletinMessage msg, bool content = false)
        {
            if (ns == null)
            {
                return;
            }

            var longestTextLine = 0;
            var maxLength = 22;
            var poster = msg.PostedName ?? "";
            poster.UpdateLengthCounters(ref maxLength, ref longestTextLine);

            var subject = msg.Subject ?? "";
            subject.UpdateLengthCounters(ref maxLength, ref longestTextLine);

            var time = msg.GetTimeAsString() ?? "";
            time.UpdateLengthCounters(ref maxLength, ref longestTextLine);

            var equipLength = Math.Min(255, msg.PostedEquip.Length);
            var linesLength = Math.Min(255, msg.Lines.Length);

            if (content)
            {
                maxLength += 2 + equipLength * 4; // We have an extra 4 from the thread serial
                for (int i = 0; i < linesLength; i++)
                {
                    msg.Lines[i].UpdateLengthCounters(ref maxLength, ref longestTextLine, true);
                }
            }

            Span<byte> textBuffer = stackalloc byte[TextEncoding.UTF8.GetMaxByteCount(longestTextLine)];

            var writer = maxLength > 81920 ? new SpanWriter(maxLength) : new SpanWriter(stackalloc byte[maxLength]);
            writer.Write((byte)0x71); // Packet ID
            writer.Seek(2, SeekOrigin.Current);
            writer.Write((byte)(content ? 0x02 : 0x01)); // Command
            writer.Write(board.Serial);
            writer.Write(msg.Serial);
            if (!content)
            {
                writer.Write(msg.Thread?.Serial ?? Serial.Zero);
            }

            writer.WriteString(poster, textBuffer);
            writer.WriteString(subject, textBuffer);
            writer.WriteString(time, textBuffer);

            if (content)
            {
                writer.Write((short)msg.PostedBody);
                writer.Write((short)msg.PostedHue);

                writer.Write((byte)equipLength);
                for (var i = 0; i < equipLength; i++)
                {
                    var eq = msg.PostedEquip[i];
                    writer.Write((short)eq.itemID);
                    writer.Write((short)eq.hue);
                }

                writer.Write((byte)linesLength);
                for (var i = 0; i < linesLength; i++)
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
            var tail = pad ? 2 : 1;
            var length = Math.Min(pad ? 253 : 254, text.GetBytesUtf8(buffer));
            writer.Write((byte)(length + tail));
            writer.Write(buffer.SliceToLength(length));

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
