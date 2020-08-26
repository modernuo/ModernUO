using System;

namespace Server.Items
{
  [Flippable(0x2D1E, 0x2D2A)]
  public class ElvenCompositeLongbow : BaseRanged
  {
    [Constructible]
    public ElvenCompositeLongbow() : base(0x2D1E) => Weight = 8.0;

    public ElvenCompositeLongbow(Serial serial) : base(serial)
    {
    }

    public override int EffectID => 0xF42;
    public override Type AmmoType => typeof(Arrow);
    public override Item Ammo => new Arrow();

    public override WeaponAbility PrimaryAbility => WeaponAbility.ForceArrow;
    public override WeaponAbility SecondaryAbility => WeaponAbility.SerpentArrow;

    public override int AosStrengthReq => 45;
    public override int AosMinDamage => 12;
    public override int AosMaxDamage => 16;
    public override int AosSpeed => 27;
    public override float MlSpeed => 4.00f;

    public override int OldStrengthReq => 45;
    public override int OldMinDamage => 12;
    public override int OldMaxDamage => 16;
    public override int OldSpeed => 27;

    public override int DefMaxRange => 10;

    public override int InitMinHits => 41;
    public override int InitMaxHits => 90;

    public override WeaponAnimation DefAnimation => WeaponAnimation.ShootBow;

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