namespace Server.Items
{
  [Flippable(0x143B, 0x143A)]
  public class Maul : BaseBashing
  {
    [Constructible]
    public Maul() : base(0x143B) => Weight = 10.0;

    public Maul(Serial serial) : base(serial)
    {
    }

    public override WeaponAbility PrimaryAbility => WeaponAbility.CrushingBlow;
    public override WeaponAbility SecondaryAbility => WeaponAbility.ConcussionBlow;

    public override int AosStrengthReq => 45;
    public override int AosMinDamage => 14;
    public override int AosMaxDamage => 16;
    public override int AosSpeed => 32;
    public override float MlSpeed => 3.50f;

    public override int OldStrengthReq => 20;
    public override int OldMinDamage => 10;
    public override int OldMaxDamage => 30;
    public override int OldSpeed => 30;

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

      if (Weight == 14.0)
        Weight = 10.0;
    }
  }
}