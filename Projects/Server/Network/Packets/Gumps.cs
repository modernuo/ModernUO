using System;

namespace Server.Network
{
  public static partial class Packets
  {
    public static void SendCloseGump(NetState ns, int typeId, int buttonId)
    {
      SpanWriter w = new SpanWriter(ns.SendPipe.Writer.GetSpan(13));
      w.Write((byte)0xBF); // Packet ID
      w.Write((short)13); // Length

      w.Write((short)0x04); // Command
      w.Write(typeId);
      w.Write(buttonId);

      _ = ns.Flush(13);
    }
  }
}
