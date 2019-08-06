using System;

namespace Server.Network
{
  public enum TradeFlag : byte
  {
    Display = 0x0,
    Close = 0x1,
    Update = 0x2,
    UpdateGold = 0x3,
    UpdateLedger = 0x4
  }

  public static partial class Packets
  {
    public static WriteDynamicPacketMethod<Serial, Serial, Serial, string> DisplaySecureTrade(out int length, Serial them, Serial firstCont, Serial secondCont, string name)
    {
      length = 18 + name?.Length ?? 0;

      static int write(Memory<byte> mem, int length, Serial them, Serial firstCont, Serial secondCont, string name)
      {
        SpanWriter w = new SpanWriter(mem.Span, length);
        w.Write((byte)0x6F); // Packet ID
        w.Write((ushort)length); // Length

        w.Position++; // Display
        w.Write(them);
        w.Write(firstCont);
        w.Write(secondCont);
        w.Write((byte)1);
        w.WriteAsciiFixed(name ?? "", 30);

        return length;
      }

      return write;
    }

    public static WriteFixedPacketMethod<Serial> CloseSecureTrade(out int length)
    {
      length = 8;

      static void write(Memory<byte> mem, Serial cont)
      {
        SpanWriter w = new SpanWriter(mem.Span, 8);
        w.Write((byte)0x6F); // Packet ID
        w.Write((ushort)8); // Length

        w.Write((byte)1); // Close
        w.Write(cont);
      }

      return write;
    }

    public static WriteFixedPacketMethod<Serial, TradeFlag, int, int> UpdateSecureTrade(out int length)
    {
      length = 17;

      static void write(Memory<byte> mem, Serial cont, TradeFlag flag, int first, int second)
      {
        SpanWriter w = new SpanWriter(mem.Span, 17);
        w.Write((byte)0x6F); // Packet ID
        w.Write((ushort)17); // Length

        w.Write((byte)flag);
        w.Write(cont);
        w.Write(first);
        w.Write(second);
      }

      return write;
    }

    public static WriteFixedPacketMethod<Item, Serial> SecureTradeEquip(out int length)
    {
      length = 20;

      static void write(Memory<byte> mem, Item item, Serial m)
      {
        SpanWriter w = new SpanWriter(mem.Span, 20);
        w.Write((byte)0x25); // Packet ID

        w.Write(item.Serial);
        w.Write((short)item.ItemID);
        w.Position++; // Write((byte)0)
        w.Write((short)item.Amount);
        w.Write((short)item.X);
        w.Write((short)item.Y);
        w.Write(m);
        w.Write((short)item.Hue);
      }

      return write;
    }

    public static WriteFixedPacketMethod<Item, Serial> SecureTradeEquip6017(out int length)
    {
      length = 21;

      static void write(Memory<byte> mem, Item item, Serial m)
      {
        SpanWriter w = new SpanWriter(mem.Span, 21);
        w.Write((byte)0x25); // Packet ID

        w.Write(item.Serial);
        w.Write((short)item.ItemID);
        w.Position++; // Write((byte)0)
        w.Write((short)item.Amount);
        w.Write((short)item.X);
        w.Write((short)item.Y);
        w.Write(m);
        w.Position++; // Write((byte)0) Grid Location?
        w.Write((short)item.Hue);
      }

      return write;
    }
  }
}
