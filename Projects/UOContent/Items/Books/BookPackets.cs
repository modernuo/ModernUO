/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
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
        public static void SendBookPageDetails(this NetState ns, BaseBook book)
        {
            if (ns == null)
            {
                return;
            }

            var maxLength = 9 + ;
            var writer = new SpanWriter(stackalloc byte[38]);
            writer.Write((byte)0x66); // Packet ID
            writer.Seek(2, SeekOrigin.Current);
            writer.Write(book.Serial);

            var count = book.PagesCount;
            writer.Write((ushort)count);

            for (var i = 0; i < count; ++i)
            {
                var page = book.Pages[i];

                writer.Write((ushort)(i + 1));
                writer.Write((ushort)page.Lines.Length);

                for (var j = 0; j < page.Lines.Length; ++j)
                {
                    var buffer = page.Lines[j].GetBytesUtf8();

                    writer.Write(buffer, 0, buffer.Length);
                    writer.Write((byte)0);
                }
            }

            writer.WritePacketLength();
            ns.Send(writer.Span);
        }
    }
}
