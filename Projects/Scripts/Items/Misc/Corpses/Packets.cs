using System.Collections.Generic;
using Server.Buffers;
using Server.Items;

namespace Server.Network
{
  public static class CorpsePackets
  {
    public static void SendCorpseEquip(NetState ns, Mobile beholder, Corpse beheld)
    {
      if (ns == null)
        return;

      List<Item> list = beheld.EquipItems;

      int length = 8 + list.Count * 5;
      if (beheld.Hair?.ItemID > 0)
        length += 5;
      if (beheld.FacialHair?.ItemID > 0)
        length += 5;

      SpanWriter writer = new SpanWriter(stackalloc byte[length]);
      writer.Write((byte)0x89); // Packet ID
      writer.Write((ushort)length); // Dynamic Length

      writer.Write(beheld.Serial);

      for (int i = 0; i < list.Count; ++i)
      {
        Item item = list[i];

        if (!item.Deleted && beholder.CanSee(item) && item.Parent == beheld)
        {
          writer.Write((byte)(item.Layer + 1));
          writer.Write(item.Serial);
        }
      }

      if (beheld.Hair?.ItemID > 0)
      {
        writer.Write((byte)(Layer.Hair + 1));
        writer.Write(HairInfo.FakeSerial(beheld.Owner.Serial) - 2);
      }

      if (beheld.FacialHair?.ItemID > 0)
      {
        writer.Write((byte)(Layer.FacialHair + 1));
        writer.Write(FacialHairInfo.FakeSerial(beheld.Owner.Serial) - 2);
      }

      writer.Position++; // writer.Write((byte)Layer.Invalid);

      ns.Send(writer.Span);
    }

    public static void SendCorpseContent(NetState ns, Mobile beholder, Corpse corpse)
    {
      if (ns == null)
        return;

      if (ns.ContainerGridLines)
        SendCorpseContentNew(ns, beholder, corpse);
      else
        SendCorpseContentOld(ns, beholder, corpse);
    }

    public static void SendCorpseContentOld(NetState ns, Mobile beholder, Corpse corpse)
    {
      if (ns == null)
        return;

      List<Item> items = corpse.EquipItems;
      int length = 5 + items.Count * 19;

      if (corpse.Hair?.ItemID > 0)
        length += 19;
      if (corpse.FacialHair?.ItemID > 0)
        length += 19;

      SpanWriter writer = new SpanWriter(stackalloc byte[length]);
      writer.Write((byte)0x3C); // Packet ID
      writer.Write((ushort)length); // Dynamic Length
      writer.Position += 2;

      ushort written = 0;

      for (int i = 0; i < items.Count; ++i)
      {
        Item child = items[i];

        if (!child.Deleted && child.Parent == corpse && beholder.CanSee(child))
        {
          writer.Write(child.Serial);
          writer.Write((ushort)child.ItemID);
          writer.Position++; // signed, itemID offset
          writer.Write((ushort)child.Amount);
          writer.Write((short)child.X);
          writer.Write((short)child.Y);
          writer.Write(corpse.Serial);
          writer.Write((ushort)child.Hue);

          ++written;
        }
      }

      if (corpse.Hair?.ItemID > 0)
      {
        writer.Write(HairInfo.FakeSerial(corpse.Owner.Serial) - 2);
        writer.Write((ushort)corpse.Hair.ItemID);
        writer.Position++; // signed, itemID offset
        writer.Write((ushort)1);
        writer.Position += 4;
        writer.Write(corpse.Serial);
        writer.Write((ushort)corpse.Hair.Hue);

        ++written;
      }

      if (corpse.FacialHair?.ItemID > 0)
      {
        writer.Write(FacialHairInfo.FakeSerial(corpse.Owner.Serial) - 2);
        writer.Write((ushort)corpse.FacialHair.ItemID);
        writer.Position++; // signed, itemID offset
        writer.Write((ushort)1);
        writer.Position += 4;
        writer.Write(corpse.Serial);
        writer.Write((ushort)corpse.FacialHair.Hue);

        ++written;
      }

      writer.Position = 3;
      writer.Write(written);

      ns.Send(writer.Span);
    }

    public static void SendCorpseContentNew(NetState ns, Mobile beholder, Corpse corpse)
    {
      if (ns == null)
        return;

      List<Item> items = corpse.EquipItems;
      int length = 5 + items.Count * 20;

      if (corpse.Hair?.ItemID > 0)
        length += 20;
      if (corpse.FacialHair?.ItemID > 0)
        length += 20;

      SpanWriter writer = new SpanWriter(stackalloc byte[length]);
      writer.Write((byte)0x3C); // Packet ID
      writer.Write((ushort)length); // Dynamic Length
      writer.Position += 2;

      ushort written = 0;

      for (int i = 0; i < items.Count; ++i)
      {
        Item child = items[i];

        if (!child.Deleted && child.Parent == corpse && beholder.CanSee(child))
        {
          writer.Write(child.Serial);
          writer.Write((ushort)child.ItemID);
          writer.Position++; // signed, itemID offset
          writer.Write((ushort)child.Amount);
          writer.Write((short)child.X);
          writer.Write((short)child.Y);
          writer.Position++; // grid location
          writer.Write(corpse.Serial);
          writer.Write((ushort)child.Hue);

          ++written;
        }
      }

      if (corpse.Hair?.ItemID > 0)
      {
        writer.Write(HairInfo.FakeSerial(corpse.Owner.Serial) - 2);
        writer.Write((ushort)corpse.Hair.ItemID);
        writer.Position++; // signed, itemID offset
        writer.Write((ushort)1);
        writer.Position += 5;
        writer.Write(corpse.Serial);
        writer.Write((ushort)corpse.Hair.Hue);

        ++written;
      }

      if (corpse.FacialHair?.ItemID > 0)
      {
        writer.Write(FacialHairInfo.FakeSerial(corpse.Owner.Serial) - 2);
        writer.Write((ushort)corpse.FacialHair.ItemID);
        writer.Position++; // signed, itemID offset
        writer.Write((ushort)1);
        writer.Position += 5;
        writer.Write(corpse.Serial);
        writer.Write((ushort)corpse.FacialHair.Hue);

        ++written;
      }

      writer.Position = 3;
      writer.Write(written);

      ns.Send(writer.Span);
    }
  }
}
