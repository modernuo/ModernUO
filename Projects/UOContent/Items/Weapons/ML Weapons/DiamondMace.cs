namespace Server.Items
{
  [Flippable(0x2D24, 0x2D30)]
  public class DiamondMace : BaseBashing
  {
    [Constructible]
    public DiamondMace() : base(0x2D24) => Weight = 10.0;

    public DiamondMace(Serial serial) : base(serial)
    {
    }

    public override WeaponAbility PrimaryAbility => WeaponAbility.ConcussionBlow;
    public override WeaponAbility SecondaryAbility => WeaponAbility.CrushingBlow;

    public override int AosStrengthReq => 35;
    public override int AosMinDamage => 14;
    public override int AosMaxDamage => 17;
    public override int AosSpeed => 37;
    public override float MlSpeed => 3.00f;

    public override int OldStrengthReq => 35;
    public override int OldMinDamage => 14;
    public override int OldMaxDamage => 17;
    public override int OldSpeed => 37;

    public override int InitMinHits => 30; // TODO
    public override int InitMaxHits => 60; // TODO

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.WriteEncodedInt(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadEncodedInt();
    }
  }
}