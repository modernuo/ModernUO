namespace Server.Items
{
  public class OverseerSunderedBlade : RadiantScimitar
  {
    [Constructible]
    public OverseerSunderedBlade()
    {
      ItemID = 0x2D27;
      Hue = 0x485;

      Attributes.RegenStam = 2;
      Attributes.AttackChance = 10;
      Attributes.WeaponSpeed = 35;
      Attributes.WeaponDamage = 45;

      Hue = GetElementalDamageHue();
    }

    public OverseerSunderedBlade(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1072920; // Overseer Sundered Blade

    public override void GetDamageTypes(Mobile wielder, out int phys, out int fire, out int cold, out int pois,
      out int nrgy, out int chaos, out int direct)
    {
      phys = cold = pois = nrgy = chaos = direct = 0;
      fire = 100;
    }

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