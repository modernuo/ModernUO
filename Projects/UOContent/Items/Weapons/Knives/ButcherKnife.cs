namespace Server.Items
{
  [Flippable(0x13F6, 0x13F7)]
  public class ButcherKnife : BaseKnife
  {
    [Constructible]
    public ButcherKnife() : base(0x13F6) => Weight = 1.0;

    public ButcherKnife(Serial serial) : base(serial)
    {
    }

    public override WeaponAbility PrimaryAbility => WeaponAbility.InfectiousStrike;
    public override WeaponAbility SecondaryAbility => WeaponAbility.Disarm;

    public override int AosStrengthReq => 5;
    public override int AosMinDamage => 9;
    public override int AosMaxDamage => 11;
    public override int AosSpeed => 49;
    public override float MlSpeed => 2.25f;

    public override int OldStrengthReq => 5;
    public override int OldMinDamage => 2;
    public override int OldMaxDamage => 14;
    public override int OldSpeed => 40;

    public override int InitMinHits => 31;
    public override int InitMaxHits => 40;

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