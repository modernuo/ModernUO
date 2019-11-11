using System;
using System.Collections.Generic;
using Server.Buffers;

namespace Server.Network
{
  public static partial class Packets
  {
    public static void SendDisplayContainer(NetState ns, Serial s, short gumpId = -1)
    {
      if (ns == null)
        return;

      if (ns.HighSeas)
        SendDisplayContainerHS(ns, s, gumpId);
      else
        SendDisplayContainerOld(ns, s, gumpId);
    }

    public static void SendDisplayContainerOld(NetState ns, Serial s, short gumpId = -1)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[7]);
      w.Write((byte)0x24); // Packet ID

      w.Write(s);
      w.Write(gumpId);

      ns.Send(w.RawSpan);
    }

    public static void SendDisplayContainerHS(NetState ns, Serial s, short gumpId = -1)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[9]);
      w.Write((byte)0x24); // Packet ID

      w.Write(s);
      w.Write(gumpId);
      w.Write((short)0x7D);

      ns.Send(w.RawSpan);
    }

    public static void SendContainerContentUpdate(NetState ns, Item item)
    {
      if (ns == null)
        return;

      if (ns.ContainerGridLines)
        SendContainerContentUpdateNew(ns, item);
      else
        SendContainerContentUpdateOld(ns, item);
    }

    public static void SendContainerContentUpdateOld(NetState ns, Item item)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[20]);
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

      ns.Send(w.RawSpan);
    }

    public static void SendContainerContentUpdateNew(NetState ns, Item item)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[21]);
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

      ns.Send(w.RawSpan);
    }

    public static void SendContainerContent(NetState ns, Mobile beholder, Item beheld)
    {
      if (ns == null)
        return;

      if (ns.ContainerGridLines)
        SendContainerContentNew(ns, beholder, beheld);
      else
        SendContainerContentOld(ns, beholder, beheld);
    }

    public static void SendContainerContentOld(NetState ns, Mobile beholder, Item beheld)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[5 + beheld.Items.Count * 19]);
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

      w.Position = 1;
      w.Write((ushort)w.WrittenCount);
      w.Write(written);

      ns.Send(w.Span);
    }

    public static void SendContainerContentNew(NetState ns, Mobile beholder, Item beheld)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[5 + beheld.Items.Count * 20]);
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

      w.Position = 1;
      w.Write((ushort)w.WrittenCount);
      w.Write(written);

      ns.Send(w.Span);
    }
  }
}
