using System;
using System.Collections.Generic;
using System.IO;

namespace Server.Network
{
  public static partial class Packets
  {
    public static WriteFixedPacketMethod<Serial, int> DisplayContainer(out int length)
    {
      length = 7;

      static void write(Memory<byte> mem, Serial s, int gumpid)
      {
        SpanWriter w = new SpanWriter(mem.Span, 7);
        w.Write((byte)0x24); // Packet ID


        w.Write(s);
        w.Write((short)gumpid);
      }

      return write;
    }

    public static WriteFixedPacketMethod<Serial, int> DisplayContainerHS(out int length)
    {
      length = 9;

      static void write(Memory<byte> mem, Serial s, int gumpid)
      {
        SpanWriter w = new SpanWriter(mem.Span, 9);
        w.Write((byte)0x24); // Packet ID


        w.Write(s);
        w.Write((short)gumpid);
        w.Write((short)0x7D);
      }

      return write;
    }

    public static WriteFixedPacketMethod<Item> ContainerContentUpdate(out int length)
    {
      length = 20;

      static void write(Memory<byte> mem, Item item)
      {
        SpanWriter w = new SpanWriter(mem.Span, 20);
        w.Write((byte)0x25); // Packet ID

        Serial parentSerial;

        if (item.Parent is Item parentItem)
          parentSerial = parentItem.Serial;
        else
        {
          Console.WriteLine("Warning: ContainerContentUpdate on item with !(parent is Item)");
          parentSerial = Serial.Zero;
        }

        w.Write(item.Serial);
        w.Write((ushort)item.ItemID);
        w.Position++; // w.Write((byte)0); // signed, itemID offset
        w.Write((ushort)item.Amount);
        w.Write((short)item.X);
        w.Write((short)item.Y);
        w.Write(parentSerial);
        w.Write((ushort)(item.QuestItem ? Item.QuestItemHue : item.Hue));
      }

      return write;
    }

    public static WriteFixedPacketMethod<Item> ContainerContentUpdate6017(out int length)
    {
      length = 21;

      static void write(Memory<byte> mem, Item item)
      {
        SpanWriter w = new SpanWriter(mem.Span, 21);
        w.Write((byte)0x25); // Packet ID

        Serial parentSerial;

        if (item.Parent is Item parentItem)
          parentSerial = parentItem.Serial;
        else
        {
          Console.WriteLine("Warning: ContainerContentUpdate on item with !(parent is Item)");
          parentSerial = Serial.Zero;
        }

        w.Write(item.Serial);
        w.Write((ushort)item.ItemID);
        w.Position++; // signed, itemID offset
        w.Write((ushort)item.Amount);
        w.Write((short)item.X);
        w.Write((short)item.Y);
        w.Position++; // Grid Location?
        w.Write(parentSerial);
        w.Write((ushort)(item.QuestItem ? Item.QuestItemHue : item.Hue));
      }

      return write;
    }

    public static WriteDynamicPacketMethod<Mobile, Item> ContainerContent(out int length, Mobile beholder, Item beheld)
    {
      length = 5 + beheld.Items.Count * 19;

      static int write(Memory<byte> mem, int length, Mobile beholder, Item beheld)
      {
        SpanWriter w = new SpanWriter(mem.Span, length);
        w.Write((byte)0x3C); // Packet ID
        w.Position += 4; // Dynamic Length, Item Count

        List<Item> items = beheld.Items;
        int count = items.Count;

        ushort written = 0;

        for (int i = 0; i < count; ++i)
        {
          Item child = items[i];

          if (!child.Deleted && beholder.CanSee(child))
          {
            Point3D loc = child.Location;

            w.Write(child.Serial);
            w.Write((ushort)child.ItemID);
            w.Position++; // signed, itemID offset
            w.Write((ushort)child.Amount);
            w.Write((short)loc.m_X);
            w.Write((short)loc.m_Y);
            w.Write(beheld.Serial);
            w.Write((ushort)(child.QuestItem ? Item.QuestItemHue : child.Hue));

            ++written;
          }
        }

        int bytesWritten = w.Position;
        w.Seek(1, SeekOrigin.Begin);
        w.Write((ushort)bytesWritten);
        w.Write(written);

        return bytesWritten;
      }

      return write;
    }

    public static WriteDynamicPacketMethod<Mobile, Item> ContainerContent6017(out int length, Mobile beholder, Item beheld)
    {
      length = 5 + beheld.Items.Count * 20;

      static int write(Memory<byte> mem, int length, Mobile beholder, Item beheld)
      {
        SpanWriter w = new SpanWriter(mem.Span, length);
        w.Write((byte)0x3C); // Packet ID
        w.Position += 4; // Dynamic Length, Item Count

        List<Item> items = beheld.Items;
        int count = items.Count;

        ushort written = 0;

        for (int i = 0; i < count; ++i)
        {
          Item child = items[i];

          if (!child.Deleted && beholder.CanSee(child))
          {
            Point3D loc = child.Location;

            w.Write(child.Serial);
            w.Write((ushort)child.ItemID);
            w.Position++; // signed, itemID offset
            w.Write((ushort)child.Amount);
            w.Write((short)loc.m_X);
            w.Write((short)loc.m_Y);
            w.Position++; // Grid Location?
            w.Write(beheld.Serial);
            w.Write((ushort)(child.QuestItem ? Item.QuestItemHue : child.Hue));

            ++written;
          }
        }

        int bytesWritten = w.Position;
        w.Seek(1, SeekOrigin.Begin);
        w.Write((ushort)bytesWritten);
        w.Write(written);

        return bytesWritten;
      }

      return write;
    }
  }
}
