using Server.Gumps;

namespace Server.Items
{
  public class ResGate : Item
  {
    [Constructible]
    public ResGate() : base(0xF6C)
    {
      Movable = false;
      Hue = 0x2D1;
      Light = LightType.Circle300;
    }

    public ResGate(Serial serial) : base(serial)
    {
    }

    public override string DefaultName => "a resurrection gate";

    public override bool OnMoveOver(Mobile m)
    {
      if (!m.Alive && m.Map?.CanFit(m.Location, 16, false, false) == true)
      {
        m.PlaySound(0x214);
        m.FixedEffect(0x376A, 10, 16);

        m.CloseGump<ResurrectGump>();
        m.SendGump(new ResurrectGump(m));
      }
      else
      {
        m.SendLocalizedMessage(502391); // Thou can not be resurrected there!
      }

      return false;
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }
}