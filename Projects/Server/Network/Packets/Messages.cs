using System;
using System.Collections.Generic;
using Server.Buffers;

namespace Server.Network.Packets
{
  [Flags]
  public enum AffixType : byte
  {
    Append = 0x00,
    Prepend = 0x01,
    System = 0x02
  }

  public static partial class Packets
  {
    private static readonly Dictionary<int, byte[]> _messageLocalizedPackets = new Dictionary<int, byte[]>();

    public static void SendMessageLocalized(NetState ns, int number)
    {
      if (ns == null)
        return;

      if (_messageLocalizedPackets.TryGetValue(number, out var packet))
      {
        ns.Send(packet);
        return;
      }

      SpanWriter w = new SpanWriter(packet = new byte[50]);
      w.Write((byte)0xC1); // Packet ID
      w.Write((short)50); // Length

      w.Write(Serial.MinusOne);
      w.Write((short)-1);
      w.Position++; // w.Write((byte)MessageType.Regular);
      w.Write((short)0x3B2);
      w.Write((short)3);
      w.Write(number);
      w.WriteAsciiFixed("System", 30);

      _messageLocalizedPackets[number] = packet;

      ns.Send(packet);
    }

    public static void SendMessageLocalized(NetState ns, Serial serial, int graphic, MessageType type, int hue, int font, int number, string name,
      string args)
    {
      if (ns == null)
        return;

      int length = 50 + args.Length * 2;

      SpanWriter w = new SpanWriter(stackalloc byte[length]);
      w.Write((byte)0xC1); // Packet ID
      w.Write((short)length); // Length

      if (hue == 0) hue = 0x3B2;

      w.Write(serial);
      w.Write((short)graphic);
      w.Write((byte)type);
      w.Write((short)hue);
      w.Write((short)font);
      w.Write(number);
      w.WriteAsciiFixed(name ?? "", 30);
      w.WriteLittleUniNull(args ?? "");

      ns.Send(w.RawSpan);
    }

    public static void SendMessageLocalizedAffix(NetState ns, Serial serial, int graphic, MessageType type, int hue, int font, int number,
      string name, AffixType affixType, string affix, string args)
    {
      if (ns == null)
        return;

      affix ??= "";
      args ??= "";

      if (hue == 0) hue = 0x3B2;

      int length = 52 + affix.Length + args.Length * 2;

      SpanWriter w = new SpanWriter(stackalloc byte[length]);
      w.Write((byte)0xCC); // Packet ID
      w.Write((short)length); // Length

      w.Write(serial);
      w.Write((short)graphic);
      w.Write((byte)type);
      w.Write((short)hue);
      w.Write((short)font);
      w.Write(number);
      w.Write((byte)affixType);
      w.WriteAsciiFixed(name ?? "", 30);
      w.WriteAsciiNull(affix);
      w.WriteBigUniNull(args); // Not little endian? Ugh

      ns.Send(w.RawSpan);
    }

    // TODO: Handle IEnumerable<NetState>
    public static void SendAsciiMessage(NetState ns, Serial serial, int graphic, MessageType type, int hue, int font, string name, string text)
    {
      if (ns == null)
        return;

      text ??= "";

      if (hue == 0) hue = 0x3B2;

      int length = 45 + text.Length;

      SpanWriter w = new SpanWriter(stackalloc byte[length]);
      w.Write((byte)0x1C); // Packet ID
      w.Write((short)length); // Length

      w.Write(serial);
      w.Write((short)graphic);
      w.Write((byte)type);
      w.Write((short)hue);
      w.Write((short)font);
      w.WriteAsciiFixed(name ?? "", 30);
      w.WriteAsciiNull(text);

      ns.Send(w.RawSpan);
    }

    // TODO: Handle IEnumerable<NetState>
    public static void SendUnicodeMessage(NetState ns, Serial serial, int graphic, MessageType type, int hue, int font, string lang, string name,
      string text)
    {
      if (ns == null)
        return;

      if (string.IsNullOrEmpty(lang)) lang = "ENU";
      name ??= "";
      text ??= "";
      if (hue == 0) hue = 0x3B2;

      int length = 50 + text.Length * 2;

      SpanWriter w = new SpanWriter(stackalloc byte[length]);
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

      ns.Send(w.RawSpan);
    }
  }
}
