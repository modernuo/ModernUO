using System;
using Server.Buffers;
using Server.Network;

namespace Server.Items
{
  public static class BookPackets
  {
    public static void SendBookPageDetails(NetState ns, BaseBook book)
    {
      if (ns == null)
        return;

      // 80 chars per line (240 bytes)
      // 8 lines per page
      // Max: 77049 bytes
      int length = 9;

      for (int i = 0; i < book.PagesCount; i++)
        length += 1926 * book.Pages[i].Lines.Length;

      SpanWriter writer = new SpanWriter(stackalloc byte[length]);
      writer.Write((byte)0x66); // Packet ID
      writer.Position += 2; // Dynamic Length

      writer.Write(book.Serial);
      writer.Write((ushort)book.PagesCount);

      for (int i = 0; i < book.PagesCount; i++)
      {
        BookPageInfo page = book.Pages[i];
        length = Math.Min(page.Lines.Length, 8);

        writer.Write((ushort)(i + 1));
        writer.Write((ushort)length);

        for (int j = 0; j < length; j++)
        {
          string line = page.Lines[j];
          writer.WriteUTF8Null(line);
        }
      }

      writer.Position = 1;
      writer.Write((ushort)writer.WrittenCount);

      ns.Send(writer.Span);
    }

    public static void SendBookHeader(NetState ns, Mobile from, BaseBook book)
    {
      if (ns == null)
        return;

      string title = book.Title ?? "";
      string author = book.Author ?? "";

      int length = 17 + title.Length + author.Length;
      SpanWriter writer = new SpanWriter(stackalloc byte[length]);
      writer.Write((byte)0xD4); // Packet ID
      writer.Position += 2; // Dynamic Length

      writer.Write(book.Serial);
      writer.Write((byte)0x1); // true
      writer.Write(book.Writable && from.InRange(book.GetWorldLocation(), 1));
      writer.Write((ushort)book.PagesCount);

      writer.Write((ushort)title.Length + 1);
      writer.WriteAsciiNull(title);

      writer.Write((ushort)author.Length + 1);
      writer.WriteAsciiNull(author);

      writer.Position = 1;
      writer.Write((ushort)writer.WrittenCount);

      ns.Send(writer.Span);
    }
  }
}
