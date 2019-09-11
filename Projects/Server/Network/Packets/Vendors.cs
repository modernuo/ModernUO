using System.Collections.Generic;
using Server.Buffers;
using Server.Items;

namespace Server.Network
{
  public static partial class Packets
  {
    public static void SendVendorBuyContent(NetState ns, IList<BuyItemState> list)
    {
      if (ns == null)
        return;

      int length = 5 + list.Count * 19;
      SpanWriter w = new SpanWriter(stackalloc byte[length]);
      w.Write((byte)0x3C); // Packet ID
      w.Write((ushort)length); // Length

      w.Write((short)list.Count);

      //The client sorts these by their X/Y value.
      //OSI sends these in weird order.  X/Y highest to lowest and serial lowest to highest
      //These are already sorted by serial (done by the vendor class) but we have to send them by x/y
      //(the x74 packet is sent in 'correct' order.)
      for (int i = list.Count - 1; i >= 0; --i)
      {
        BuyItemState bis = list[i];

        w.Write(bis.MySerial);
        w.Write((ushort)bis.ItemID);
        w.Position++; // Write((byte)0); itemid offset
        w.Write((ushort)bis.Amount);
        w.Write((short)(i + 1)); //x
        w.Write((short)1); //y
        w.Write(bis.ContainerSerial);
        w.Write((ushort)bis.Hue);
      }

      ns.Send(w.RawSpan);
    }

    public static void SendVendorBuyContent6017(NetState ns, IList<BuyItemState> list)
    {
      if (ns == null)
        return;

      int length = 5 + list.Count * 20;
      SpanWriter w = new SpanWriter(stackalloc byte[length]);
      w.Write((byte)0x3C); // Packet ID
      w.Write((ushort)length); // Length

      w.Write((short)list.Count);

      //The client sorts these by their X/Y value.
      //OSI sends these in weird order.  X/Y highest to lowest and serial lowest to highest
      //These are already sorted by serial (done by the vendor class) but we have to send them by x/y
      //(the x74 packet is sent in 'correct' order.)
      for (int i = list.Count - 1; i >= 0; --i)
      {
        BuyItemState bis = list[i];

        w.Write(bis.MySerial);
        w.Write((ushort)bis.ItemID);
        w.Position++; // Write((byte)0); itemid offset
        w.Write((ushort)bis.Amount);
        w.Write((short)(i + 1)); //x
        w.Write((short)1); //y
        w.Position++; // Write((byte)0); Grid Location?
        w.Write(bis.ContainerSerial);
        w.Write((ushort)bis.Hue);
      }

      ns.Send(w.RawSpan);
    }

    public static void SendDisplayBuyList(NetState ns, Serial vendor)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[7]);
      w.Write((byte)0x24); // Packet ID

      w.Write(vendor);
      w.Write((short)0x30); // buy window id?

      ns.Send(w.RawSpan);
    }

    public static void SendDisplayBuyListHS(NetState ns, Serial vendor)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[9]);
      w.Write((byte)0x24); // Packet ID

      w.Write(vendor);
      w.Write((short)0x30); // buy window id?
      //w.Write((short)0x00); // Unknown

      ns.Send(w.RawSpan);
    }

    public static void SendVendorBuyList(NetState ns, Mobile vendor, IList<BuyItemState> list)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[8 + 135 * list.Count]);
      w.Write((byte)0x74); // Packet ID
      w.Position += 2; // Dynamic Length

      w.Write(!(vendor.FindItemOnLayer(Layer.ShopBuy) is Container BuyPack) ? Serial.MinusOne : BuyPack.Serial);

      w.Write((byte)list.Count);

      for (int i = 0; i < list.Count; ++i)
      {
        BuyItemState bis = list[i];

        w.Write(bis.Price);

        string desc = bis.Description ?? "";
        w.Write((byte)(desc.Length + 1));
        w.WriteAsciiNull(desc);
      }

      w.Position = 1;
      w.Write((ushort)w.WrittenCount);

      ns.Send(w.Span);
    }

    public static void SendVendorSellList(NetState ns, Serial shopkeeper, IList<SellItemState> list)
    {
      if (ns == null)
        return;

      ushort count = (ushort)list.Count;
      SpanWriter w = new SpanWriter(stackalloc byte[9 + 140 * count]);
      w.Write((byte)0x9E); // Packet ID
      w.Position += 2; //( Dynamic Length

      w.Write(shopkeeper);

      w.Write(count);

      for (int i = 0; i < count; ++i)
      {
        SellItemState state = list[i];

        w.Write(state.Item.Serial);
        w.Write((ushort)state.Item.ItemID);
        w.Write((ushort)state.Item.Hue);
        w.Write((ushort)state.Item.Amount);
        w.Write((ushort)state.Price);

        string name = state.Item.Name;

        if (name == null || (name = name.Trim()).Length <= 0)
          name = state.Name ?? "";

        w.Write((ushort)name.Length);
        w.WriteAsciiFixed(name, (ushort)name.Length);
      }

      w.Position = 1;
      w.Write((ushort)w.WrittenCount);

      ns.Send(w.RawSpan);
    }

    public static void SendEndVendorBuyOrSell(NetState ns, Serial vendor)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[8]);
      w.Write((byte)0x3B); // Packet ID

      w.Write(vendor);

      ns.Send(w.RawSpan);
    }
  }
}
