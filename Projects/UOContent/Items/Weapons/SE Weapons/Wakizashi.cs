namespace Server.Items
{
  [Flippable(0x27A4, 0x27EF)]
  public class Wakizashi : BaseSword
  {
    [Constructible]
    public Wakizashi() : base(0x27A4)
    {
      Weight = 5.0;
      Layer = Layer.OneHanded;
    }

    public Wakizashi(Serial serial) : base(serial)
    {
    }

    public override WeaponAbility PrimaryAbility => WeaponAbility.FrenziedWhirlwind;
    public override WeaponAbility SecondaryAbility => WeaponAbility.DoubleStrike;

    public override int AosStrengthReq => 20;
    public override int AosMinDamage => 11;
    public override int AosMaxDamage => 13;
    public override int AosSpeed => 44;
    public override float MlSpeed => 2.50f;

    public override int OldStrengthReq => 20;
    public override int OldMinDamage => 11;
    public override int OldMaxDamage => 13;
    public override int OldSpeed => 44;

    public override int DefHitSound => 0x23B;
    public override int DefMissSound => 0x23A;

    public override int InitMinHits => 45;
    public override int InitMaxHits => 50;

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