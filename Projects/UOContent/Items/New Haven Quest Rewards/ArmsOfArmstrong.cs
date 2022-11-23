using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class ArmsOfArmstrong : LeatherArms
{
    [Constructible]
    public ArmsOfArmstrong()
    {
        LootType = LootType.Blessed;

        Attributes.BonusStr = 3;
        Attributes.RegenHits = 1;
    }

    public override int LabelNumber => 1077675; // Arms of Armstrong

    public override int BasePhysicalResistance => 6;
    public override int BaseFireResistance => 6;
    public override int BaseColdResistance => 5;
    public override int BasePoisonResistance => 5;
    public override int BaseEnergyResistance => 5;
}
