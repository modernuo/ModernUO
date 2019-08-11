namespace Server.Network
{
  public static partial class Packets
  {
    public static void SendMessageLocalized(NetState ns, Serial serial, int graphic, MessageType type, int hue, int font, int number, string name,
      string args)
    {
      SpanWriter w = new SpanWriter(ns.SendPipe.Writer.GetSpan(50 + args.Length * 2));
      w.Write((byte)0xC1); // Packet ID
      w.Position += 2; // Dynamic Length

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

    public static void SendAsciiMessage(NetState ns, Serial serial, int graphic, MessageType type, int hue, int font, string name, string text)
    {
      if (text == null) text = "";
      if (hue == 0) hue = 0x3B2;

      int length = 45 + text.Length;

      SpanWriter w = new SpanWriter(ns.SendPipe.Writer.GetSpan(length));
      w.Write((byte)0x1C); // Packet ID
      w.Write((short)length); // Length

      w.Write(serial);
      w.Write((short)graphic);
      w.Write((byte)type);
      w.Write((short)hue);
      w.Write((short)font);
      w.WriteAsciiFixed(name ?? "", 30);
      w.WriteAsciiNull(text);

      _ = ns.Flush(length);
    }

    public static void SendUnicodeMessage(NetState ns, Serial serial, int graphic, MessageType type, int hue, int font, string lang, string name,
      string text)
    {
      if (string.IsNullOrEmpty(lang)) lang = "ENU";
      if (name == null) name = "";
      if (text == null) text = "";
      if (hue == 0) hue = 0x3B2;

      int length = 50 + text.Length * 2;

      SpanWriter w = new SpanWriter(ns.SendPipe.Writer.GetSpan(length));
      w.Write((byte)0xAE); // Packet ID
      w.Write((short)length); // Length

      w.Write(serial);
      w.Write((short)graphic);
      w.Write((byte)type);
      w.Write((short)hue);
      w.Write((short)font);
      w.WriteAsciiFixed(lang, 4);
      w.WriteAsciiFixed(name, 30);
      w.WriteBigUniNull(text);

      _ = ns.Flush(length);
    }
  }
}
