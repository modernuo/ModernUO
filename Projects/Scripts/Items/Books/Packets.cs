using System;
using System.Buffers;
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

      Tuple<byte[], int>[] buffers = new Tuple<byte[], int>[book.PagesCount];
      SpanWriter sw = new SpanWriter(stackalloc byte[0x20003]);
      for (int i = 0; i < book.PagesCount; i++)
      {
        sw.Position = 0;
        BookPageInfo page = book.Pages[i];

        sw.Write((ushort)(i + 1));
        sw.Write((ushort)page.Lines.Length);

        for (int j = 0; j < page.Lines.Length; j++)
          sw.Position += Utility.UTF8.GetBytes(page.Lines[j].AsSpan(), sw.Span) + 1;

        // TODO: A better way?
        byte[] buffer = ArrayPool<byte>.Shared.Rent(sw.Position);
        sw.CopyTo(buffer);
        buffers[i] = Tuple.Create(buffer, sw.Position);
        length += sw.Position;
      }

      // If our packet length is more than 4/5th of the stack size (1MB), then don't stack alloc.
      SpanWriter writer = new SpanWriter(length > 838860 ? new byte[length] : stackalloc byte[length]);
      writer.Write((byte)0x66); // Packet ID
      writer.Write((ushort)length); // Dynamic Length

      writer.Write(book.Serial);
      writer.Write((ushort)book.PagesCount);

      for (int i = 0; i < buffers.Length; i++)
      {
        byte[] buffer = buffers[i].Item1;
        writer.Write(buffer.AsSpan(0, buffers[i].Item2));
        ArrayPool<byte>.Shared.Return(buffer);
      }

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

      int length = 17 + titleBuffer.Length + authorBuffer.Length;
      SpanWriter writer = new SpanWriter(stackalloc byte[length]);
      writer.Write((byte)0xD4); // Packet ID
      writer.Write((ushort)length); // Dynamic Length

      writer.Write(book.Serial);
      writer.Write((byte)0x1); // true
      writer.Write(book.Writable && from.InRange(book.GetWorldLocation(), 1));
      writer.Write((ushort)book.PagesCount);

      writer.Write((ushort)(titleBuffer.Length + 1));
      writer.Write(titleBuffer);
      writer.Position++; // writer.Write((byte)0); // terminate

      writer.Write((ushort)(authorBuffer.Length + 1));
      writer.Write(authorBuffer);
      writer.Position++; // writer.Write((byte)0); // terminate

      ns.Send(writer.Span);
    }
  }
}
