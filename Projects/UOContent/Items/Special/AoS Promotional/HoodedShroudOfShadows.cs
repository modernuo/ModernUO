namespace Server.Items
{
  [Flippable(0x2684, 0x2683)]
  public class HoodedShroudOfShadows : BaseOuterTorso
  {
    [Constructible]
    public HoodedShroudOfShadows(int hue = 0x455) : base(0x2684, hue)
    {
      LootType = LootType.Blessed;
      Weight = 3.0;
    }

    public HoodedShroudOfShadows(Serial serial) : base(serial)
    {
    }

    public override bool Dye(Mobile from, DyeTub sender)
    {
      from.SendLocalizedMessage(sender.FailMessage);
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
