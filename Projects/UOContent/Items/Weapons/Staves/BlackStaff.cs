namespace Server.Items
{
  [Flippable(0xDF1, 0xDF0)]
  public class BlackStaff : BaseStaff
  {
    [Constructible]
    public BlackStaff() : base(0xDF0) => Weight = 6.0;

    public BlackStaff(Serial serial) : base(serial)
    {
    }

    public override WeaponAbility PrimaryAbility => WeaponAbility.WhirlwindAttack;
    public override WeaponAbility SecondaryAbility => WeaponAbility.ParalyzingBlow;

    public override int AosStrengthReq => 35;
    public override int AosMinDamage => 13;
    public override int AosMaxDamage => 16;
    public override int AosSpeed => 39;
    public override float MlSpeed => 2.75f;

    public override int OldStrengthReq => 35;
    public override int OldMinDamage => 8;
    public override int OldMaxDamage => 33;
    public override int OldSpeed => 35;

    public override int InitMinHits => 31;
    public override int InitMaxHits => 70;

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