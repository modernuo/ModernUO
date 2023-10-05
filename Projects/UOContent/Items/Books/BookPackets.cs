/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BookPackets.cs                                                  *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Buffers;
using System.IO;
using Server.Network;
using Server.Text;

namespace Server.Items
{
    public static class BookPackets
    {
        public static unsafe void Configure()
        {
            IncomingPackets.Register(0xD4, 0, true, &HeaderChange);
            IncomingPackets.Register(0x66, 0, true, &ContentChange);
            IncomingPackets.Register(0x93, 99, true, &OldHeaderChange);
        }

        public static void OldHeaderChange(NetState state, SpanReader reader, int packetLength)
        {
            var from = state.Mobile;

            if (World.FindItem((Serial)reader.ReadUInt32()) is not BaseBook book || !book.Writable ||
                !from.InRange(book.GetWorldLocation(), 1) || !book.IsAccessibleTo(from))
            {
                return;
            }

            reader.Seek(4, SeekOrigin.Current); // Skip flags and page count

            var title = reader.ReadAsciiSafe(60);
            var author = reader.ReadAsciiSafe(30);

            book.Title = Utility.FixHtml(title);
            book.Author = Utility.FixHtml(author);
        }

        public static void HeaderChange(NetState state, SpanReader reader, int packetLength)
        {
            var from = state.Mobile;

            if (World.FindItem((Serial)reader.ReadUInt32()) is not BaseBook book || !book.Writable ||
                !from.InRange(book.GetWorldLocation(), 1) || !book.IsAccessibleTo(from))
            {
                return;
            }

            reader.Seek(4, SeekOrigin.Current); // Skip flags and page count

            int titleLength = reader.ReadUInt16();

            if (titleLength > 60)
            {
                return;
            }

            // TODO: Read string to a Span<char> stackalloc instead of a returned value
            // This way we can avoid an allocation and do Utility.FixHtml against it by searching/replacing characters
            var title = reader.ReadUTF8Safe(titleLength);

            int authorLength = reader.ReadUInt16();

            if (authorLength > 30)
            {
                return;
            }

            var author = reader.ReadUTF8Safe(authorLength);

            book.Title = Utility.FixHtml(title);
            book.Author = Utility.FixHtml(author);
        }

        public static void ContentChange(NetState state, SpanReader reader, int packetLength)
        {
            var from = state.Mobile;

            if (World.FindItem((Serial)reader.ReadUInt32()) is not BaseBook book || !book.Writable ||
                !from.InRange(book.GetWorldLocation(), 1) || !book.IsAccessibleTo(from))
            {
                return;
            }

            int pageCount = reader.ReadUInt16();

            if (pageCount > book.PagesCount)
            {
                return;
            }

            for (var i = 0; i < pageCount; ++i)
            {
                int index = reader.ReadUInt16();

                if (index < 1 || index > book.PagesCount)
                {
                    return;
                }

                --index;

                int lineCount = reader.ReadUInt16();

                if (lineCount > 8)
                {
                    return;
                }

                var lines = new string[lineCount];

                for (var j = 0; j < lineCount; ++j)
                {
                    if ((lines[j] = reader.ReadUTF8Safe()).Length >= 80)
                    {
                        return;
                    }
                }

                book.Pages[index].Lines = lines;
            }
        }

        public static void SendBookContent(this NetState ns, BaseBook book)
        {
            if (ns.CannotSendPackets())
            {
                return;
            }

            var count = book.PagesCount;

            // Practical limit is 24 full pages at full unicode, or 96 full pages at ascii-only
            // For pure ascii, finding byte count is fast enough.
            // If perf becomes an issue, then switch to resizable SpanWriter
            var length = 9;
            for (var i = 0; i < count; i++)
            {
                var page = book.Pages[i];
                length += 4;
                // max is 8
                for (var j = 0; j < page.Lines.Length; j++)
                {
                    length += TextEncoding.UTF8.GetByteCount(page.Lines[j]) + 1;
                }
            }

            var writer = new SpanWriter(stackalloc byte[length]);
            writer.Write((byte)0x66); // Packet ID
            writer.Write((ushort)length);
            writer.Write(book.Serial);
            writer.Write((ushort)count);

            for (var i = 0; i < count; i++)
            {
                var page = book.Pages[i];

                writer.Write((ushort)(i + 1));
                writer.Write((ushort)page.Lines.Length);

                for (var j = 0; j < page.Lines.Length; j++)
                {
                    writer.WriteUTF8Null(page.Lines[j]);
                }
            }

            writer.WritePacketLength();

            ns.Send(writer.Span);
        }

        public static void SendBookCover(this NetState ns, Mobile from, BaseBook book)
        {
            if (ns.CannotSendPackets())
            {
                return;
            }

            var title = book.Title ?? "";
            var titleLength = TextEncoding.UTF8.GetByteCount(title);

            var author = book.Author ?? "";
            var authorLength = TextEncoding.UTF8.GetByteCount(author);

            var length = 17 + titleLength + authorLength;
            var writer = new SpanWriter(stackalloc byte[length]);
            writer.Write((byte)0xD4); // Packet ID
            writer.Write((ushort)length);
            writer.Write(book.Serial);
            writer.Write((byte)0x1); // Flag on
            writer.Write(book.Writable && from.InRange(book.GetWorldLocation(), 1));
            writer.Write((ushort)book.PagesCount);

            writer.Write((ushort)(titleLength + 1));
            writer.WriteUTF8Null(title);

            writer.Write((ushort)(authorLength + 1));
            writer.WriteUTF8Null(author);

            ns.Send(writer.Span);
        }
    }
}
