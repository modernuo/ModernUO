namespace Server.Items
{
  [Flippable(0x2D25, 0x2D31)]
  public class WildStaff : BaseStaff
  {
    [Constructible]
    public WildStaff() : base(0x2D25) => Weight = 8.0;

    public WildStaff(Serial serial) : base(serial)
    {
    }

    public override WeaponAbility PrimaryAbility => WeaponAbility.Block;
    public override WeaponAbility SecondaryAbility => WeaponAbility.ForceOfNature;

    public override int AosStrengthReq => 15;
    public override int AosMinDamage => 10;
    public override int AosMaxDamage => 12;
    public override int AosSpeed => 48;
    public override float MlSpeed => 2.25f;

    public override int OldStrengthReq => 15;
    public override int OldMinDamage => 10;
    public override int OldMaxDamage => 12;
    public override int OldSpeed => 48;

    public override int InitMinHits => 30;
    public override int InitMaxHits => 60;

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