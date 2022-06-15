using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class RingOfTheVile : GoldRing
{
    [Constructible]
    public RingOfTheVile()
    {
        Hue = 0x4F7;
        Attributes.BonusDex = 8;
        Attributes.RegenStam = 6;
        Attributes.AttackChance = 15;
        Resistances.Poison = 20;
    }

    public override int LabelNumber => 1061102; // Ring of the Vile
    public override int ArtifactRarity => 11;
}
