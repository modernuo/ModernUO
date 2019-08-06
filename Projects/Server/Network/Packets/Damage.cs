using System;

namespace Server.Network
{
  public static partial class Packets
  {
    public static WriteFixedPacketMethod<Serial, int> DamageOld(out int length)
    {
      length = 11;

      static void write(Memory<byte> mem, Serial m, int amount)
      {
        SpanWriter w = new SpanWriter(mem.Span, 11);

        w.Write((byte)0xBF); // Packet ID
        w.Write((ushort)11); // Length

        w.Write((short)0x22);
        w.Write((byte)1);
        w.Write(m);

        if (amount > 255)
          amount = 255;
        else if (amount < 0)
          amount = 0;

        w.Write((byte)amount);
      }

      return write;
    }

    public static WriteFixedPacketMethod<Serial, int> Damage(out int length)
    {
      length = 7;

      static void write(Memory<byte> mem, Serial m, int amount)
      {
        SpanWriter w = new SpanWriter(mem.Span, 7);

        w.Write((byte)0xBF); // Packet ID
        w.Write((ushort)7); // Length

        w.Write(m);

        if (amount > 0xFFFF)
          amount = 0xFFFF;
        else if (amount < 0)
          amount = 0;

        w.Write((ushort)amount);
      }

      return write;
    }
  }
}
