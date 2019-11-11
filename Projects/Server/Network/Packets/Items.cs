using Server.Buffers;
using Server.Items;

namespace Server.Network
{
  public enum LRReason : byte
  {
    CannotLift,
    OutOfRange,
    OutOfSight,
    TryToSteal,
    AreHolding,
    Unspecific
  }

  public static partial class Packets
  {
    public static void SendWorldItem(NetState ns, Item item)
    {
      if (ns == null)
        return;

      if (ns.HighSeas)
        SendWorldItemHS(ns, item);
      else if (ns.StygianAbyss)
        SendWorldItemSA(ns, item);
      else
        SendWorldItemOld(ns, item);
    }

    public static void SendWorldItemOld(NetState ns, Item item)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[20]);
      w.Write((byte)0x1A); // Packet ID
      w.Position += 2; // Dynamic Length

      uint serial = item.Serial.Value;
      int itemId = item.ItemID & 0x3FFF;
      int amount = item.Amount;
      Point3D loc = item.Location;
      int x = loc.m_X;
      int y = loc.m_Y;
      int hue = item.Hue;
      int flags = item.GetPacketFlags();
      int direction = (int)item.Direction;

      if (amount != 0)
        serial |= 0x80000000;
      else
        serial &= 0x7FFFFFFF;

      w.Write(serial);

      if (item is BaseMulti)
        w.Write((short)(itemId | 0x4000));
      else
        w.Write((short)itemId);

      if (amount != 0)
        w.Write((short)amount);

      x &= 0x7FFF;

      if (direction != 0) x |= 0x8000;

      w.Write((short)x);

      y &= 0x3FFF;

      if (hue != 0) y |= 0x8000;

      if (flags != 0) y |= 0x4000;

      w.Write((short)y);

      if (direction != 0)
        w.Write((byte)direction);

      w.Write((sbyte)loc.m_Z);

      if (hue != 0)
        w.Write((ushort)hue);

      if (flags != 0)
        w.Write((byte)flags);

      w.Position = 1;
      w.Write((ushort)w.WrittenCount);

      ns.Send(w.Span);
    }

    public static void SendWorldItemSA(NetState ns, Item item)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[24]);
      w.Write((byte)0xF3); // Packet ID

      w.Write((short)0x01);

      int itemId = item.ItemID;

      if (item is BaseMulti)
      {
        w.Write((byte)0x02);

        w.Write(item.Serial);

        itemId &= 0x3FFF;

        w.Write((short)itemId);

        w.Position++; // w.Write((byte)0);
      }
      else
      {
        w.Position++; // w.Write((byte)0);

        w.Write(item.Serial);

        itemId &= 0x7FFF;

        w.Write((short)itemId);

        w.Write((byte)item.Direction);
      }

      short amount = (short)item.Amount;
      w.Write(amount);
      w.Write(amount);

      Point3D loc = item.Location;
      w.Write((short)(loc.m_X & 0x7FFF));
      w.Write((short)(loc.m_Y & 0x3FFF));
      w.Write((sbyte)loc.m_Z);

      w.Write((byte)item.Light);
      w.Write((short)item.Hue);
      w.Write((byte)item.GetPacketFlags());

      ns.Send(w.Span);
    }

    public static void SendWorldItemHS(NetState ns, Item item)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[26]);
      w.Write((byte)0xF3); // Packet ID

      w.Write((short)0x01);

      int itemId = item.ItemID;

      if (item is BaseMulti)
      {
        w.Write((byte)0x02);

        w.Write(item.Serial);

        itemId &= 0x3FFF;

        w.Write((short)itemId);

        w.Position++; // w.Write((byte)0);
      }
      else
      {
        w.Position++; // w.Write((byte)0);

        w.Write(item.Serial);

        itemId &= 0x7FFF;

        w.Write((short)itemId);

        w.Write((byte)item.Direction);
      }

      short amount = (short)item.Amount;
      w.Write(amount);
      w.Write(amount);

      Point3D loc = item.Location;
      w.Write((short)(loc.m_X & 0x7FFF));
      w.Write((short)(loc.m_Y & 0x3FFF));
      w.Write((sbyte)loc.m_Z);

      w.Write((byte)item.Light);
      w.Write((short)item.Hue);
      w.Write((byte)item.GetPacketFlags());
      w.Position += 2;

      ns.Send(w.Span);
    }

    public static void SendLiftRej(NetState ns, LRReason reason)
    {
      ns?.Send(stackalloc byte[]
      {
        0x27, // Packet ID
        (byte)reason
      });
    }

    public static void SendSpellbookContent(NetState ns, Serial s, int count, int offset, ulong content)
    {
      if (ns == null)
        return;

      if (ObjectPropertyList.Enabled && ns.NewSpellbook)
        SendSpellbookContentNew(ns, s, count, offset, content);
      else if (ns.ContainerGridLines)
        SendSpellbookContent6017(ns, s, count, offset, content);
      else
        SendSpellbookContentOld(ns, s, count, offset, content);
    }

    public static void SendSpellbookContentOld(NetState ns, Serial s, int count, int offset, ulong content)
    {
      if (ns == null)
        return;

      int length = 5 + count * 19;
      SpanWriter w = new SpanWriter(stackalloc byte[length]);
      w.Write((byte)0x3C); // Packet ID
      w.Write((short)length); // Length

      // This should always be the same as written
      w.Write((ushort)count);

      ulong mask = 1;

      for (int i = 0; i < 64; ++i, mask <<= 1)
        if ((content & mask) != 0)
        {
          w.Write(0x7FFFFFFF - i);
          w.Position += 3;
          w.Write((ushort)(i + offset));
          w.Position += 4;
          w.Write(s);
          w.Position += 2;
        }

      ns.Send(w.Span);
    }

    public static void SendSpellbookContent6017(NetState ns, Serial s, int count, int offset, ulong content)
    {
      if (ns == null)
        return;

      int length = 5 + count * 20;
      SpanWriter w = new SpanWriter(stackalloc byte[length]);
      w.Write((byte)0x3C); // Packet ID
      w.Write((short)length); // Length

      // This should always be the same as written
      w.Write((ushort)count);

      ulong mask = 1;

      for (int i = 0; i < 64; ++i, mask <<= 1)
        if ((content & mask) != 0)
        {
          w.Write(0x7FFFFFFF - i);
          w.Position += 3;
          w.Write((ushort)(i + offset));
          w.Position += 5; // Grid Location?
          w.Write(s);
          w.Position += 2;
        }

      ns.Send(w.Span);
    }

    public static void SendSpellbookContentNew(NetState ns, Serial s, int graphic, int offset, ulong content)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[23]);
      w.Write((byte)0x3C); // Packet ID
      w.Write((short)23); // Length


      w.Write((short)0x1B);
      w.Write((short)0x01);

      w.Write(s);
      w.Write((short)graphic);
      w.Write((short)offset);

      for (int i = 0; i < 8; ++i)
        w.Write((byte)(content >> (i * 8)));

      ns.Send(w.Span);
    }

    public static void SendDragEffect(NetState ns, IEntity src, IEntity trg, int itemID, int hue, int amount)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[26]);
      w.Write((byte)0x23); // Packet ID

      w.Write((short)itemID);
      w.Position++; // w.Write((byte)0);
      w.Write((short)hue);
      w.Write((short)amount);
      w.Write(src.Serial);
      w.Write((short)src.X);
      w.Write((short)src.Y);
      w.Write((sbyte)src.Z);
      w.Write(trg.Serial);
      w.Write((short)trg.X);
      w.Write((short)trg.Y);
      w.Write((sbyte)trg.Z);

      ns.Send(w.Span);
    }
  }
}
