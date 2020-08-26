namespace Server.Items
{
  public class GreaterHealPotion : BaseHealPotion
  {
    [Constructible]
    public GreaterHealPotion() : base(PotionEffect.HealGreater)
    {
    }

    public GreaterHealPotion(Serial serial) : base(serial)
    {
    }

    public override int MinHeal => Core.AOS ? 20 : 9;
    public override int MaxHeal => Core.AOS ? 25 : 30;
    public override double Delay => 10.0;

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