using System;
using System.Collections.Generic;

namespace Server.Network
{
  public interface IGumpWriter
  {
    int TextEntries { get; set; }
    int Switches { get; set; }

    void AppendLayout(bool val);
    void AppendLayout(int val);
    void AppendLayout(uint val);
    void AppendLayoutNS(int val);
    void AppendLayout(string text);
    void AppendLayout(byte[] buffer);
    void WriteStrings(List<string> strings);
    void Flush();
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

      ns.SendCompressed(w.Span);
    }
  }
}
