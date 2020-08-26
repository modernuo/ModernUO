namespace Server.Items
{
  [Flippable(0x1403, 0x1402)]
  public class ShortSpear : BaseSpear
  {
    [Constructible]
    public ShortSpear() : base(0x1403) => Weight = 4.0;

    public ShortSpear(Serial serial) : base(serial)
    {
    }

    public override WeaponAbility PrimaryAbility => WeaponAbility.ShadowStrike;
    public override WeaponAbility SecondaryAbility => WeaponAbility.MortalStrike;

    public override int AosStrengthReq => 40;
    public override int AosMinDamage => 10;
    public override int AosMaxDamage => 13;
    public override int AosSpeed => 55;
    public override float MlSpeed => 2.00f;

    public override int OldStrengthReq => 15;
    public override int OldMinDamage => 4;
    public override int OldMaxDamage => 32;
    public override int OldSpeed => 50;

    public override int InitMinHits => 31;
    public override int InitMaxHits => 70;

    public override WeaponAnimation DefAnimation => WeaponAnimation.Pierce1H;

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