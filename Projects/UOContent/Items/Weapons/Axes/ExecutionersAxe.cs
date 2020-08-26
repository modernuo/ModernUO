namespace Server.Items
{
  [Flippable(0xf45, 0xf46)]
  public class ExecutionersAxe : BaseAxe
  {
    [Constructible]
    public ExecutionersAxe() : base(0xF45) => Weight = 8.0;

    public ExecutionersAxe(Serial serial) : base(serial)
    {
    }

    public override WeaponAbility PrimaryAbility => WeaponAbility.BleedAttack;
    public override WeaponAbility SecondaryAbility => WeaponAbility.MortalStrike;

    public override int AosStrengthReq => 40;
    public override int AosMinDamage => 15;
    public override int AosMaxDamage => 17;
    public override int AosSpeed => 33;
    public override float MlSpeed => 3.25f;

    public override int OldStrengthReq => 35;
    public override int OldMinDamage => 6;
    public override int OldMaxDamage => 33;
    public override int OldSpeed => 37;

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