using Server.Buffers;

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
    public static void SendDisplaySecureTrade(NetState ns, Serial them, Serial firstCont, Serial secondCont, string name)
    {
      int length = 18 + name?.Length ?? 0;
      SpanWriter w = new SpanWriter(stackalloc byte[length]);
      w.Write((byte)0x6F); // Packet ID
      w.Write((short)length); // Length

      w.Position++; // Display
      w.Write(them);
      w.Write(firstCont);
      w.Write(secondCont);
      w.Write((byte)1);
      w.WriteAsciiFixed(name ?? "", 30);

      ns.Send(w.Span);
    }

    public static void SendCloseSecureTrade(NetState ns, Serial cont)
    {
      SpanWriter w = new SpanWriter(stackalloc byte[8]);
      w.Write((byte)0x6F); // Packet ID
      w.Write((ushort)8); // Length

      w.Write((byte)1); // Close
      w.Write(cont);

      ns.Send(w.Span);
    }

    public static void SendUpdateSecureTrade(NetState ns, Serial cont, TradeFlag flag, int first, int second)
    {
      SpanWriter w = new SpanWriter(stackalloc byte[17]);
      w.Write((byte)0x6F); // Packet ID
      w.Write((ushort)17); // Length

      w.Write((byte)flag);
      w.Write(cont);
      w.Write(first);
      w.Write(second);

      ns.Send(w.Span);
    }

    public static void SendSecureTradeEquip(NetState ns, Item item, Serial m)
    {
      SpanWriter w = new SpanWriter(stackalloc byte[20]);
      w.Write((byte)0x25); // Packet ID

      w.Write(item.Serial);
      w.Write((short)item.ItemID);
      w.Position++; // Write((byte)0)
      w.Write((short)item.Amount);
      w.Write((short)item.X);
      w.Write((short)item.Y);
      w.Write(m);
      w.Write((short)item.Hue);

      ns.Send(w.Span);
    }

    public static void SendSecureTradeEquip6017(NetState ns, Item item, Serial m)
    {
      SpanWriter w = new SpanWriter(stackalloc byte[21]);
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

      ns.Send(w.Span);
    }
  }
}
