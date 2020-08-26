namespace Server.Items
{
  public class TheDryadBow : Bow
  {
    private static readonly SkillName[] m_PossibleBonusSkills =
    {
      SkillName.Archery,
      SkillName.Healing,
      SkillName.MagicResist,
      SkillName.Peacemaking,
      SkillName.Chivalry,
      SkillName.Ninjitsu
    };

    [Constructible]
    public TheDryadBow()
    {
      ItemID = 0x13B1;
      Hue = 0x48F;
      SkillBonuses.SetValues(0, m_PossibleBonusSkills.RandomElement(), Utility.Random(4) == 0 ? 10.0 : 5.0);
      WeaponAttributes.SelfRepair = 5;
      Attributes.WeaponSpeed = 50;
      Attributes.WeaponDamage = 35;
      WeaponAttributes.ResistPoisonBonus = 15;
    }

    public TheDryadBow(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1061090; // The Dryad Bow
    public override int ArtifactRarity => 11;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(1);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      if (version < 1)
        SkillBonuses.SetValues(0, m_PossibleBonusSkills.RandomElement(), Utility.Random(4) == 0 ? 10.0 : 5.0);
    }
  }
}
