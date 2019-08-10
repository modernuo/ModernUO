using System;
using System.Collections.Generic;
using System.IO;

namespace Server.Network
{
  public static partial class Packets
  {
    public static void SendMessageLocalized(NetState ns, Serial serial, int graphic, MessageType type, int hue, int font, int number, string name, string args)
    {
      SpanWriter w = new SpanWriter(ns.SendPipe.Writer.GetSpan(50 + (args.Length * 2)));
      w.Write((byte)0xC1); // Packet ID

      if (hue == 0)
        hue = 0x3B2;

      w.Write(serial);
      w.Write((short)graphic);
      w.Write((byte)type);
      w.Write((short)hue);
      w.Write((short)font);
      w.Write(number);
      w.WriteAsciiFixed(name ?? "", 30);
      w.WriteLittleUniNull(args ?? "");

      int bytesWritten = w.Position;
      w.Position = 1;
      w.Write((ushort)bytesWritten);

      _ = ns.Flush(bytesWritten);
    }
  }
}
