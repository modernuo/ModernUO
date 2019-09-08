using Server.Gumps;
using Server.Buffers;
using System.Collections.Generic;
using System.Buffers;

namespace Server.Network
{
  public interface IGumpWriter
  {
    int TextEntries { get; set; }
    int Switches { get; set; }
    ArrayBufferWriter<byte> Layout { get; }

    int Intern(string value);
    void WriteStrings(List<string> strings);
    void Flush(NetState ns);
  }

  public static partial class Packets
  {
    public static void SendCloseGump(NetState ns, int typeId, int buttonId)
    {
      SpanWriter w = new SpanWriter(stackalloc byte[13]);
      w.Write((byte)0xBF); // Packet ID
      w.Write((short)13); // Length

      w.Write((short)0x04); // Command
      w.Write(typeId);
      w.Write(buttonId);

      ns.Send(w.Span);
    }

    public static void SendDisplayGump(NetState ns, Gump g, string layout, string[] text)
    {
      // Assume no longer than 128 characters per text element
      SpanWriter w = new SpanWriter(stackalloc byte[20 + layout.Length + 1 + (258 * text.Length)]);
      w.Write((byte)0xB0); // Packet ID
      w.Position += 2; // Dynamic Length

      if (layout == null) layout = "";

      w.Write(g.Serial);
      w.Write(g.TypeID);
      w.Write(g.X);
      w.Write(g.Y);
      w.Write((ushort)(layout.Length + 1));
      w.WriteAsciiNull(layout);

      w.Write((ushort)text.Length);

      for (int i = 0; i < text.Length; ++i)
      {
        string v = text[i] ?? "";

        ushort length = (ushort)v.Length;

        w.Write(length);
        w.WriteBigUniFixed(v, length);
      }

      int bytesWritten = w.Position;
      w.Position = 1;
      w.Write((ushort)bytesWritten);

      ns.Send(w.Span.Slice(0, bytesWritten));
    }

    public static void SendDisplaySignGump(NetState ns, Serial serial, int gumpID, string unknown, string caption)
    {
      int length = 14 + caption.Length + unknown.Length;
      SpanWriter w = new SpanWriter(stackalloc byte[length]);
      w.Write((byte)0x8B); // Packet ID
      w.Write((short)length);

      w.Write(serial);
      w.Write((short)gumpID);
      w.Write((short)unknown.Length);
      w.WriteAsciiFixed(unknown, unknown.Length);
      w.Write((short)(caption.Length + 1));
      w.WriteAsciiFixed(caption, caption.Length);

      ns.Send(w.Span);
    }
  }
}
