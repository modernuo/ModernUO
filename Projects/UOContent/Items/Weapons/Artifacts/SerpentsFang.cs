namespace Server.Items
{
  public class SerpentsFang : Kryss
  {
    [Constructible]
    public SerpentsFang()
    {
      ItemID = 0x1400;
      Hue = 0x488;
      WeaponAttributes.HitPoisonArea = 100;
      WeaponAttributes.ResistPoisonBonus = 20;
      Attributes.AttackChance = 15;
      Attributes.WeaponDamage = 50;
    }

    public SerpentsFang(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1061601; // Serpent's Fang
    public override int ArtifactRarity => 11;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    public override void GetDamageTypes(Mobile wielder, out int phys, out int fire, out int cold, out int pois,
      out int nrgy, out int chaos, out int direct)
    {
      fire = cold = nrgy = chaos = direct = 0;
      phys = 25;
      pois = 75;
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      if (ItemID == 0x1401)
        ItemID = 0x1400;
    }
  }
}