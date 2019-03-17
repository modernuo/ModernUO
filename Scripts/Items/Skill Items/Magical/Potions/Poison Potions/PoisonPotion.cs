namespace Server.Items
{
  public class PoisonPotion : BasePoisonPotion
  {
    [Constructible]
    public PoisonPotion() : base(PotionEffect.Poison)
    {
    }

    public PoisonPotion(Serial serial) : base(serial)
    {
    }

    public override Poison Poison => Poison.Regular;

    public override int MinPoisoningSkill => 300;
    public override int MaxPoisoningSkill => 700;

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
