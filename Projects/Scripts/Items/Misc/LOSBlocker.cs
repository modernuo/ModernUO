using Server.Buffers;
using Server.Network;

namespace Server.Items
{
  public class LOSBlocker : Item
  {
    [Constructible]
    public LOSBlocker() : base(0x21A2) => Movable = false;

    public LOSBlocker(Serial serial) : base(serial)
    {
    }

    public override string DefaultName => "no line of sight";

    public static void Initialize()
    {
      TileData.ItemTable[0x21A2].Flags = TileFlag.Wall | TileFlag.NoShoot;
      TileData.ItemTable[0x21A2].Height = 20;
    }

    protected override void SendWorldPacketFor(NetState state)
    {
      Mobile mob = state.Mobile;

      if (mob?.AccessLevel >= AccessLevel.GameMaster)
      {
        if (state.HighSeas)
          SendGMItemHS(state);
        else if (state.StygianAbyss)
          SendGMItemSA(state);
        else
          SendGMItem(state);
      }

      base.SendWorldPacketFor(state);
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(1);
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      if (version < 1 && ItemID == 0x2199)
        ItemID = 0x21A2;
    }

    private void SendGMItem(NetState ns)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[20]);
      w.Write((byte)0x1A); // Packet ID
      w.Position += 2; // Dynamic Length

      uint serial = Serial.Value;
      int itemId = 0x36FF;
      int amount = Amount;
      Point3D loc = Location;
      int x = loc.X;
      int y = loc.Y;
      int hue = Hue;
      int flags = GetPacketFlags();
      int direction = (int)Direction;

      if (amount != 0)
        serial |= 0x80000000;
      else
        serial &= 0x7FFFFFFF;

      w.Write(serial);

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

      w.Write((sbyte)loc.Z);

      if (hue != 0)
        w.Write((ushort)hue);

      if (flags != 0)
        w.Write((byte)flags);

      w.Position = 1;
      w.Write((ushort)w.WrittenCount);

      ns.Send(w.Span);
    }

    public void SendGMItemSA(NetState ns)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[24]);
      w.Write((byte)0xF3); // Packet ID

      w.Write((short)0x01);

      int itemId = 0x36FF;

      w.Position++; // w.Write((byte)0);

      w.Write(Serial);

      w.Write((short)itemId);

      w.Write((byte)Direction);

      short amount = (short)Amount;
      w.Write(amount);
      w.Write(amount);

      Point3D loc = Location;
      w.Write((short)(loc.X & 0x7FFF));
      w.Write((short)(loc.Y & 0x3FFF));
      w.Write((sbyte)loc.Z);

      w.Write((byte)Light);
      w.Write((short)Hue);
      w.Write((byte)GetPacketFlags());

      ns.Send(w.RawSpan);
    }

    public void SendGMItemHS(NetState ns)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[26]);
      w.Write((byte)0xF3); // Packet ID

      w.Write((short)0x01);

      int itemId = 0x36FF;

      w.Position++; // w.Write((byte)0);

      w.Write(Serial);

      w.Write((short)itemId);

      w.Write((byte)Direction);

      short amount = (short)Amount;
      w.Write(amount);
      w.Write(amount);

      Point3D loc = Location;
      w.Write((short)(loc.X & 0x7FFF));
      w.Write((short)(loc.Y & 0x3FFF));
      w.Write((sbyte)loc.Z);

      w.Write((byte)Light);
      w.Write((short)Hue);
      w.Write((byte)GetPacketFlags());

      ns.Send(w.RawSpan);
    }
  }
}
