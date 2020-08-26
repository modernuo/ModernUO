namespace Server.Items
{
  [Flippable(0x2D28, 0x2D34)]
  public class OrnateAxe : BaseAxe
  {
    [Constructible]
    public OrnateAxe() : base(0x2D28)
    {
      Weight = 12.0;
      Layer = Layer.TwoHanded;
    }

    public OrnateAxe(Serial serial) : base(serial)
    {
    }

    public override WeaponAbility PrimaryAbility => WeaponAbility.Disarm;
    public override WeaponAbility SecondaryAbility => WeaponAbility.CrushingBlow;

    public override int AosStrengthReq => 45;
    public override int AosMinDamage => 18;
    public override int AosMaxDamage => 20;
    public override int AosSpeed => 26;
    public override float MlSpeed => 3.50f;

    public override int OldStrengthReq => 45;
    public override int OldMinDamage => 18;
    public override int OldMaxDamage => 20;
    public override int OldSpeed => 26;

    public override int DefMissSound => 0x239;

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