using System;
using System.Collections.Generic;
using System.IO;
using Server.Items;
using Server.Mobiles;

namespace Server.Network
{
  public static partial class Packets
  {
    public static WriteDynamicPacketMethod<IList<BuyItemState>> VendorBuyContent(out int length, IList<BuyItemState> list)
    {
      length = 5 + list.Count * 19;

      static int write(Memory<byte> mem, int length, IList<BuyItemState> list)
      {
        SpanWriter w = new SpanWriter(mem.Span, length);
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

        return length;
      }

      return write;
    }

    public static WriteDynamicPacketMethod<IList<BuyItemState>> VendorBuyContent6017(out int length, IList<BuyItemState> list)
    {
      length = 5 + list.Count * 20;

      static int write(Memory<byte> mem, int length, IList<BuyItemState> list)
      {
        SpanWriter w = new SpanWriter(mem.Span, length);
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

        return length;
      }

      return write;
    }

    public static WriteFixedPacketMethod<Serial> DisplayBuyList(out int length)
    {
      length = 7;

      static void write(Memory<byte> mem, Serial vendor)
      {
        SpanWriter w = new SpanWriter(mem.Span, 7);
        w.Write((byte)0x24); // Packet ID

        w.Write(vendor);
        w.Write((short)0x30); // buy window id?
      }

      return write;
    }

    public static WriteFixedPacketMethod<Serial> DisplayBuyListHS(out int length)
    {
      length = 9;

      static void write(Memory<byte> mem, Serial vendor)
      {

        SpanWriter w = new SpanWriter(mem.Span, 9);
        w.Write((byte)0x24); // Packet ID

        w.Write(vendor);
        w.Write((short)0x30); // buy window id?
        //w.Write((short)0x00); // Unknown
      }

      return write;
    }

    public static WriteDynamicPacketMethod<Mobile, IList<BuyItemState>> VendorBuyList(out int length)
    {
      length = MaxPacketSize;

      static int write(Memory<byte> mem, int length, Mobile vendor, IList<BuyItemState> list)
      {
        SpanWriter w = new SpanWriter(mem.Span, length);
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

        int bytesWritten = w.Position;
        w.Seek(1, SeekOrigin.Begin);
        w.Write((ushort)bytesWritten);

        return bytesWritten;
      }

      return write;
    }

    public static WriteDynamicPacketMethod<Serial, IList<SellItemState>> VendorSellList(out int length, Serial shopkeeper, IList<SellItemState> list)
    {
      length = MaxPacketSize;

      static int write(Memory<byte> mem, int length, Serial shopkeeper, IList<SellItemState> list)
      {
        SpanWriter w = new SpanWriter(mem.Span, length);
        w.Write((byte)0x9E); // Packet ID
        w.Position += 2; //( Dynamic Length

        w.Write(shopkeeper);

        w.Write((ushort)list.Count);

        for (int i = 0; i < list.Count; ++i)
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

        int bytesWritten = w.Position;
        w.Seek(1, SeekOrigin.Begin);
        w.Write((ushort)bytesWritten);

        return bytesWritten;
      }

      return write;
    }

    public static WriteFixedPacketMethod<Serial> EndVendorBuyOrSell(out int length)
    {
      length = 8;

      static void write(Memory<byte> mem, Serial vendor)
      {
        SpanWriter w = new SpanWriter(mem.Span, 8);
        w.Write((byte)0x3B); // Packet ID

        w.Write(vendor);
        // w.Position++; // Write((byte)0);
      }

      return write;
    }
  }
}
