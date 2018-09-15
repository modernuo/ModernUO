namespace Server.Items
{
  public class GreaterExplosionPotion : BaseExplosionPotion
  {
    [Constructible]
    public GreaterExplosionPotion() : base(PotionEffect.ExplosionGreater)
    {
    }

    public GreaterExplosionPotion(Serial serial) : base(serial)
    {
    }

    public override int MinDamage => Core.AOS ? 20 : 15;
    public override int MaxDamage => Core.AOS ? 40 : 30;

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