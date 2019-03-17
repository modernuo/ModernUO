namespace Server.Items
{
  public class DeadlyPoisonPotion : BasePoisonPotion
  {
    [Constructible]
    public DeadlyPoisonPotion() : base(PotionEffect.PoisonDeadly)
    {
    }

    public DeadlyPoisonPotion(Serial serial) : base(serial)
    {
    }

    public override Poison Poison => Poison.Deadly;

    public override int MinPoisoningSkill => 950;
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
