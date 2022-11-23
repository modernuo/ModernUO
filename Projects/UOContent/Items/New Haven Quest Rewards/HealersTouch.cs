using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class HealersTouch : LeatherGloves
{
    [Constructible]
    public HealersTouch()
    {
        LootType = LootType.Blessed;

        Attributes.BonusStam = 3;
        Attributes.ReflectPhysical = 5;
    }

    public override int LabelNumber => 1077684; // Healer's Touch

    public override int BasePhysicalResistance => 6;
    public override int BaseFireResistance => 6;
    public override int BaseColdResistance => 5;
    public override int BasePoisonResistance => 5;
    public override int BaseEnergyResistance => 5;
}
