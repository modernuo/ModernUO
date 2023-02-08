using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class EscutcheonDeAriadne : MetalKiteShield
{
    [Constructible]
    public EscutcheonDeAriadne()
    {
        LootType = LootType.Blessed;
        Hue = 0x8A5;

        ArmorAttributes.DurabilityBonus = 49;
        Attributes.ReflectPhysical = 5;
        Attributes.DefendChance = 5;
    }

    public override int LabelNumber => 1077694; // Escutcheon de Ariadne

    public override int BasePhysicalResistance => 5;
    public override int BaseEnergyResistance => 1;

    public override int AosStrReq => 14;
}
