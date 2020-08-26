namespace Server.Items
{
  public class DarkglowPotion : BasePoisonPotion
  {
    [Constructible]
    public DarkglowPotion() : base(PotionEffect.Darkglow) => Hue = 0x96;

    public DarkglowPotion(Serial serial) : base(serial)
    {
    }

    public override Poison Poison => Poison.Greater; /*  MUST be restored when prerequisites are done */

    public override double MinPoisoningSkill => 95.0;
    public override double MaxPoisoningSkill => 100.0;

    public override int LabelNumber => 1072849; // Darkglow Poison

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