using System;

namespace Server.Network
{
  public static partial class Packets
  {
    public static WriteFixedPacketMethod<Serial, Serial> DeathAnimation(out int length)
    {
      length = 13;

      static void write(Memory<byte> mem, Serial killed, Serial corpse)
      {
        SpanWriter w = new SpanWriter(mem.Span, 13);
        w.Write((byte)0xAF); // Packet ID

        w.Write(killed);
        w.Write(corpse);
        // w.Position++; w.Write(0);
      }

      return write;
    }

    public static WriteFixedPacketMethod<Mobile> StatLockInfo(out int length)
    {
      length = 12;

      static void write(Memory<byte> mem, Mobile m)
      {
        SpanWriter w = new SpanWriter(mem.Span, 13);
        w.Write((byte)0xBF); // Extended Packet ID
        w.Write((short)12); // Length

        w.Write((short)0x19); // Subcommand
        w.Write((byte)2);
        w.Write(m.Serial);
        w.Write((short)((int)m.StrLock << 4 | (int)m.DexLock << 2 | (int)m.IntLock));
      }

      return write;
    }

    public static WriteFixedPacketMethod<Serial, bool> BondStatus(out int length)
    {
      length = 11;

      static void write(Memory<byte> mem, Serial m, bool bonded)
      {
        SpanWriter w = new SpanWriter(mem.Span, 11);
        w.Write((byte)0xBF); // Extended Packet ID
        w.Write((short)11); // Length

        w.Write((short)0x19); // Command
        w.Position++; // w.Write((byte)0); // Subcommand
        w.Write(m);
        w.WriteIfTrue(bonded);
      }

      return write;
    }

    public static WriteFixedPacketMethod<Serial, sbyte> PersonalLightLevel(out int length)
    {
      length = 6;

      static void write(Memory<byte> mem, Serial m, sbyte level)
      {
        SpanWriter w = new SpanWriter(mem.Span, 6);
        w.Write((byte)0x4E); // Packet ID

        w.Write(m);
        w.Write(level);
      }

      return write;
    }

    public static WriteFixedPacketMethod<Serial> PersonalLightLevelZero(out int length)
    {
      length = 6;

      static void write(Memory<byte> mem, Serial m)
      {
        SpanWriter w = new SpanWriter(mem.Span, 6);
        w.Write((byte)0x4E); // Packet ID

        w.Write(m);
        // w.Position++ w.Write((sbyte)0);
      }

      return write;
    }

    public static WriteFixedPacketMethod<Item> EquipUpdate(out int length)
    {
      length = 15;

      static void write(Memory<byte> mem, Item item)
      {
        SpanWriter w = new SpanWriter(mem.Span, 15);
        w.Write((byte)0x2E); // Packet ID

        Serial parentSerial = Serial.Zero;
        int hue = item.Hue;

        if (item.Parent is Mobile parent)
        {
          parentSerial = parent.Serial;
          if (parent.SolidHueOverride >= 0)
            hue = parent.SolidHueOverride;
        } else
          Console.WriteLine("Warning: EquipUpdate on item with an invalid parent");

        w.Write(item.Serial);
        w.Write((short)item.ItemID);
        w.Write((short)item.Layer);
        w.Write(parentSerial);
        w.Write((short)hue);
      }

      return write;
    }

    public static WriteFixedPacketMethod<Serial, Serial> Swing(out int length)
    {
      length = 10;

      static void write(Memory<byte> mem, Serial attacker, Serial defender)
      {
        SpanWriter w = new SpanWriter(mem.Span, 10);
        w.Write((byte)0x2F); // Packet ID

        w.Position++; // ?
        w.Write(attacker);
        w.Write(defender);
      }

      return write;
    }
  }
}
