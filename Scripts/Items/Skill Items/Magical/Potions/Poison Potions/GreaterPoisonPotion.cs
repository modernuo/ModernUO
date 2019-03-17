namespace Server.Items
{
  public class GreaterPoisonPotion : BasePoisonPotion
  {
    [Constructible]
    public GreaterPoisonPotion() : base(PotionEffect.PoisonGreater)
    {
    }

    public GreaterPoisonPotion(Serial serial) : base(serial)
    {
    }

    public override Poison Poison => Poison.Greater;

    public override int MinPoisoningSkill => 600;
    public override int MaxPoisoningSkill => 1000;

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }
}
