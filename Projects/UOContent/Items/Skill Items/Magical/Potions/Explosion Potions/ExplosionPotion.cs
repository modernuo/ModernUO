namespace Server.Items
{
  public class ExplosionPotion : BaseExplosionPotion
  {
    [Constructible]
    public ExplosionPotion() : base(PotionEffect.Explosion)
    {
    }

    public ExplosionPotion(Serial serial) : base(serial)
    {
    }

    public override int MinDamage => 10;
    public override int MaxDamage => 20;

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