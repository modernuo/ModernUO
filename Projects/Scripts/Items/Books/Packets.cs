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

      int length = 9;

      for (int i = 0; i < book.PagesCount; i++)
      {
        BookPageInfo page = book.Pages[i];
        // max 80 characters per line, 2 bytes per character, null terminated
        length += 5 + page.Lines.Length * 160;
      }

      SpanWriter writer = new SpanWriter(stackalloc byte[length]);
      writer.Write((byte)0x66); // Packet ID
      writer.Position += 2; // Dynamic Length

      writer.Write(book.Serial);
      writer.Write((ushort)book.PagesCount);

      for (int i = 0; i < book.PagesCount; i++)
      {
        BookPageInfo page = book.Pages[i];

        writer.Write((ushort)(i + 1));
        writer.Write((ushort)page.Lines.Length);

        foreach (string line in page.Lines)
          writer.WriteUTF8Null(line, 80);
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

      byte[] titleBuffer = Utility.UTF8.GetBytes(title);
      byte[] authorBuffer = Utility.UTF8.GetBytes(author);

      int length = 17 + titleBuffer.Length * 2 + authorBuffer.Length * 2;
      SpanWriter writer = new SpanWriter(stackalloc byte[length]);
      writer.Write((byte)0xD4); // Packet ID
      writer.Position += 2; // Dynamic Length

      writer.Write(book.Serial);
      writer.Write((byte)0x1); // true
      writer.Write(book.Writable && from.InRange(book.GetWorldLocation(), 1));
      writer.Write((ushort)book.PagesCount);

      int pos = writer.Position;
      writer.WriteUTF8Null(title);
      length = writer.Position - pos;
      writer.Position = pos;
      writer.Write((ushort)length);

      pos = writer.Position;
      writer.WriteUTF8Null(author);
      length = writer.Position - pos;
      writer.Position = pos;
      writer.Write((ushort)length);

      writer.Position = 1;
      writer.Write((ushort)writer.WrittenCount);

      ns.Send(writer.Span);
    }
  }
}
