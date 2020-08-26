namespace Server.Items
{
  public class ParasiticPotion : BasePoisonPotion
  {
    [Constructible]
    public ParasiticPotion() : base(PotionEffect.Parasitic) => Hue = 0x17C;

    public ParasiticPotion(Serial serial) : base(serial)
    {
    }

    /* public override Poison Poison => Poison.Darkglow;  MUST be restored when prerequisites are done */
    public override Poison Poison => Poison.Greater;

    public override double MinPoisoningSkill => 95.0;
    public override double MaxPoisoningSkill => 100.0;

    public override int LabelNumber => 1072848; // Parasitic Poison

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