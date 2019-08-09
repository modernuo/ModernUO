using System;
using System.Collections.Generic;
using System.IO;

namespace Server.Network
{
  public static partial class Packets
  {
    private static Dictionary<int, Memory<byte>> m_GenericLocalizedMessages = new Dictionary<int, Memory<byte>>();

    public static ReadOnlyMemory<byte> GetGenericMessage(int number)
    {
      Memory<byte> buffer;
      if (!m_GenericLocalizedMessages.TryGetValue(number, out buffer))
      {
        buffer = new Memory<byte>(new byte[50]);
        WriteMessageLocalized(buffer, 50, Serial.MinusOne, -1, MessageType.Regular, 0x3B2, 3, number, "System", string.Empty);
        if (!m_GenericLocalizedMessages.TryAdd(number, buffer))
          Console.WriteLine("Failed to create a cached localized message packet");
      }

      return buffer;
    }

    public static WriteFixedPacketMethod<int> GenericMessageLocalized(out int length)
    {
      length = 50;

      static void write(Memory<byte> mem, int number)
      {
        GetGenericMessage(number).CopyTo(mem);
      }

      return write;
    }

    public static int WriteMessageLocalized(Memory<byte> mem, int length, Serial serial, int graphic, MessageType type, int hue, int font, int number, string name, string args)
    {
      SpanWriter w = new SpanWriter(mem.Span, length);
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
      w.Seek(1, SeekOrigin.Begin);
      w.Write((ushort)bytesWritten);

      return bytesWritten;
    }

    public static WriteDynamicPacketMethod<Serial, int, MessageType, int, int, int, string, string> MessageLocalized(out int length,
      Serial serial, int graphic, MessageType type, int hue, int font, int number, string name, string args)
    {
      length = 50 + args.Length * 2;

      return WriteMessageLocalized;
    }
  }
}
