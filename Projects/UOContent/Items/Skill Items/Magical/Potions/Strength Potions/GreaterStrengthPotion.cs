using System;

namespace Server.Items
{
  public class GreaterStrengthPotion : BaseStrengthPotion
  {
    [Constructible]
    public GreaterStrengthPotion() : base(PotionEffect.StrengthGreater)
    {
    }

    public GreaterStrengthPotion(Serial serial) : base(serial)
    {
    }

    public override int StrOffset => 20;
    public override TimeSpan Duration => TimeSpan.FromMinutes(2.0);

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