using Server.Engines.Harvest;

namespace Server.Items
{
  public class GargoylesPickaxe : BaseAxe, IUsesRemaining
  {
    [Constructible]
    public GargoylesPickaxe() : this(Utility.RandomMinMax(101, 125))
    {
    }

    [Constructible]
    public GargoylesPickaxe(int uses) : base(0xE85 + Utility.Random(2))
    {
      Weight = 11.0;
      UsesRemaining = uses;
      ShowUsesRemaining = true;
    }

    public GargoylesPickaxe(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1041281; // a gargoyle's pickaxe
    public override HarvestSystem HarvestSystem => Mining.System;

    public override WeaponAbility PrimaryAbility => WeaponAbility.DoubleStrike;
    public override WeaponAbility SecondaryAbility => WeaponAbility.Disarm;

    public override int AosStrengthReq => 50;
    public override int AosMinDamage => 13;
    public override int AosMaxDamage => 15;
    public override int AosSpeed => 35;
    public override float MlSpeed => 3.00f;

    public override int OldStrengthReq => 25;
    public override int OldMinDamage => 1;
    public override int OldMaxDamage => 15;
    public override int OldSpeed => 35;

    public override int InitMinHits => 31;
    public override int InitMaxHits => 60;

    public override WeaponAnimation DefAnimation => WeaponAnimation.Slash1H;

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      if (Hue == 0x973)
        Hue = 0x0;
    }
  }
}