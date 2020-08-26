namespace Server.Items
{
  [Flippable(0x26BE, 0x26C8)]
  public class Pike : BaseSpear
  {
    [Constructible]
    public Pike() : base(0x26BE) => Weight = 8.0;

    public Pike(Serial serial) : base(serial)
    {
    }

    public override WeaponAbility PrimaryAbility => WeaponAbility.ParalyzingBlow;
    public override WeaponAbility SecondaryAbility => WeaponAbility.InfectiousStrike;

    public override int AosStrengthReq => 50;
    public override int AosMinDamage => 14;
    public override int AosMaxDamage => 16;
    public override int AosSpeed => 37;
    public override float MlSpeed => 3.00f;

    public override int OldStrengthReq => 50;
    public override int OldMinDamage => 14;
    public override int OldMaxDamage => 16;
    public override int OldSpeed => 37;

    public override int InitMinHits => 31;
    public override int InitMaxHits => 110;

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