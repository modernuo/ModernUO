/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
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
        public static unsafe void Configure()
        {
            IncomingPackets.Register(0x71, 0, true, &BBClientRequest);
        }

        public static string FormatTS(TimeSpan ts)
        {
            var totalSeconds = (int)ts.TotalSeconds;
            var seconds = totalSeconds % 60;
            var minutes = totalSeconds / 60;

            if (minutes != 0 && seconds != 0)
            {
                return $"{minutes} minute{(minutes == 1 ? "" : "s")} and {seconds} second{(seconds == 1 ? "" : "s")}";
            }

            if (minutes != 0)
            {
                return $"{minutes} minute{(minutes == 1 ? "" : "s")}";
            }

            return $"{seconds} second{(seconds == 1 ? "" : "s")}";
        }

        public static void BBClientRequest(NetState state, SpanReader reader, int packetLength)
        {
            var from = state.Mobile;

            int packetID = reader.ReadByte();

            if (World.FindItem((Serial)reader.ReadUInt32()) is not BaseBulletinBoard board || !board.CheckRange(from))
            {
                return;
            }

            switch (packetID)
            {
                case 3:
                    BBRequestContent(from, board, reader);
                    break;
                case 4:
                    BBRequestHeader(from, board, reader);
                    break;
                case 5:
                    BBPostMessage(from, board, reader);
                    break;
                case 6:
                    BBRemoveMessage(from, board, reader);
                    break;
            }
        }

        public static void BBRequestContent(Mobile from, BaseBulletinBoard board, SpanReader reader)
        {
            if (World.FindItem((Serial)reader.ReadUInt32()) is not BulletinMessage msg || msg.Parent != board)
            {
                return;
            }

            from.NetState.SendBBMessage(board, msg, true);
        }

        public static void BBRequestHeader(Mobile from, BaseBulletinBoard board, SpanReader reader)
        {
            if (World.FindItem((Serial)reader.ReadUInt32()) is not BulletinMessage msg || msg.Parent != board)
            {
                return;
            }

            from.NetState.SendBBMessage(board, msg);
        }

        public static void BBPostMessage(Mobile from, BaseBulletinBoard board, SpanReader reader)
        {
            var thread = World.FindItem((Serial)reader.ReadUInt32()) as BulletinMessage;

            if (thread != null && thread.Parent != board)
            {
                thread = null;
            }

            var breakout = 0;

            while (thread?.Thread != null && breakout++ < 10)
            {
                thread = thread.Thread;
            }

            if (board.GetLastPostTime(from, thread == null, out var lastPostTime))
            {
                if (thread == null)
                {
                    if (!BulletinBoardSystem.CheckCreateTime(lastPostTime))
                    {
                        from.SendMessage($"You must wait {FormatTS(BulletinBoardSystem.ThreadCreateTime)} before creating a new thread.");
                        return;
                    }
                }
                else if (!BulletinBoardSystem.CheckReplyTime(lastPostTime))
                {
                    from.SendMessage($"You must wait {FormatTS(BulletinBoardSystem.ThreadReplyTime)} before replying to another thread.");
                    return;
                }
            }

            var subject = reader.ReadUTF8Safe(reader.ReadByte());

            if (subject.Length == 0)
            {
                return;
            }

            var lines = new string[reader.ReadByte()];

            if (lines.Length == 0)
            {
                return;
            }

            for (var i = 0; i < lines.Length; ++i)
            {
                lines[i] = reader.ReadUTF8Safe(reader.ReadByte());
            }

            board.PostMessage(from, thread, subject, lines);
        }

        public static void BBRemoveMessage(Mobile from, BaseBulletinBoard board, SpanReader reader)
        {
            if (World.FindItem((Serial)reader.ReadUInt32()) is not BulletinMessage msg || msg.Parent != board)
            {
                return;
            }

            if (from.AccessLevel < AccessLevel.GameMaster && msg.Poster != from)
            {
                return;
            }

            msg.Delete();
        }

        public static void SendBBDisplayBoard(this NetState ns, BaseBulletinBoard board)
        {
            if (ns.CannotSendPackets())
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
            writer.Write(textBuffer[..byteLength]);
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
            if (ns.CannotSendPackets())
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
                    writer.Write((short)eq._itemID);
                    writer.Write((short)eq._hue);
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
            writer.Write(buffer[..length]);

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
