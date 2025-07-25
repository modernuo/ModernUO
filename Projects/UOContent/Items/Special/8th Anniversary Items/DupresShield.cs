using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class DupresShield : BaseShield
{
    [Constructible]
    public DupresShield() : base(0x2B01)
    {
        LootType = LootType.Blessed;

        Attributes.BonusHits = 5;
        Attributes.RegenHits = 1;

        SkillBonuses.SetValues(0, SkillName.Parry, 5);
    }

    public override double DefaultWeight => 6.0;

    public override int LabelNumber => 1075196; // Dupre's Shield

    public override int BasePhysicalResistance => 1;
    public override int BaseFireResistance => 0;
    public override int BaseColdResistance => 0;
    public override int BasePoisonResistance => 0;
    public override int BaseEnergyResistance => 1;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    public override int AosStrReq => 50;

    public override int ArmorBase => 15;
}
