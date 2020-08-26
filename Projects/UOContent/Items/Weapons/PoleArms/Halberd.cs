namespace Server.Items
{
  [Flippable(0x143E, 0x143F)]
  public class Halberd : BasePoleArm
  {
    [Constructible]
    public Halberd() : base(0x143E) => Weight = 16.0;

    public Halberd(Serial serial) : base(serial)
    {
    }

    public override WeaponAbility PrimaryAbility => WeaponAbility.WhirlwindAttack;
    public override WeaponAbility SecondaryAbility => WeaponAbility.ConcussionBlow;

    public override int AosStrengthReq => 95;
    public override int AosMinDamage => 18;
    public override int AosMaxDamage => 19;
    public override int AosSpeed => 25;
    public override float MlSpeed => 4.25f;

    public override int OldStrengthReq => 45;
    public override int OldMinDamage => 5;
    public override int OldMaxDamage => 49;
    public override int OldSpeed => 25;

    public override int InitMinHits => 31;
    public override int InitMaxHits => 80;

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