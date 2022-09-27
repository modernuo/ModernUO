using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BurglarsBandana : Bandana
{
    [Constructible]
    public BurglarsBandana()
    {
        Hue = Utility.RandomBool() ? 0x58C : 0x10;

        SkillBonuses.SetValues(0, SkillName.Stealing, 10.0);
        SkillBonuses.SetValues(1, SkillName.Stealth, 10.0);
        SkillBonuses.SetValues(2, SkillName.Snooping, 10.0);

        Attributes.BonusDex = 5;
    }

    public override int LabelNumber => 1063473;

    public override int BasePhysicalResistance => 10;
    public override int BaseFireResistance => 5;
    public override int BaseColdResistance => 7;
    public override int BasePoisonResistance => 10;
    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
