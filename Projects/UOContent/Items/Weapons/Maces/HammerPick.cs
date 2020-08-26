namespace Server.Items
{
  [Flippable(0x143D, 0x143C)]
  public class HammerPick : BaseBashing
  {
    [Constructible]
    public HammerPick() : base(0x143D)
    {
      Weight = 9.0;
      Layer = Layer.OneHanded;
    }

    public HammerPick(Serial serial) : base(serial)
    {
    }

    public override WeaponAbility PrimaryAbility => WeaponAbility.ArmorIgnore;
    public override WeaponAbility SecondaryAbility => WeaponAbility.MortalStrike;

    public override int AosStrengthReq => 45;
    public override int AosMinDamage => 15;
    public override int AosMaxDamage => 17;
    public override int AosSpeed => 28;
    public override float MlSpeed => 3.75f;

    public override int OldStrengthReq => 35;
    public override int OldMinDamage => 6;
    public override int OldMaxDamage => 33;
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
    }
  }
}