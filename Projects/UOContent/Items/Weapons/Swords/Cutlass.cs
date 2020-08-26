namespace Server.Items
{
  [Flippable(0x1441, 0x1440)]
  public class Cutlass : BaseSword
  {
    [Constructible]
    public Cutlass() : base(0x1441) => Weight = 8.0;

    public Cutlass(Serial serial) : base(serial)
    {
    }

    public override WeaponAbility PrimaryAbility => WeaponAbility.BleedAttack;
    public override WeaponAbility SecondaryAbility => WeaponAbility.ShadowStrike;

    public override int AosStrengthReq => 25;
    public override int AosMinDamage => 11;
    public override int AosMaxDamage => 13;
    public override int AosSpeed => 44;
    public override float MlSpeed => 2.50f;

    public override int OldStrengthReq => 10;
    public override int OldMinDamage => 6;
    public override int OldMaxDamage => 28;
    public override int OldSpeed => 45;

    public override int DefHitSound => 0x23B;
    public override int DefMissSound => 0x23A;

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