using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class TunicOfGuarding : LeatherChest
{
    [Constructible]
    public TunicOfGuarding()
    {
        LootType = LootType.Blessed;

        Attributes.BonusHits = 2;
        Attributes.ReflectPhysical = 5;
    }

    public override int LabelNumber => 1077693; // Tunic of Guarding

    public override int BasePhysicalResistance => 6;
    public override int BaseFireResistance => 6;
    public override int BaseColdResistance => 5;
    public override int BasePoisonResistance => 5;
    public override int BaseEnergyResistance => 5;
}
