using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class RingOfTheElements : GoldRing
{
    [Constructible]
    public RingOfTheElements()
    {
        Hue = 0x4E9;
        Attributes.Luck = 100;
        Resistances.Fire = 16;
        Resistances.Cold = 16;
        Resistances.Poison = 16;
        Resistances.Energy = 16;
    }

    public override int LabelNumber => 1061104; // Ring of the Elements
    public override int ArtifactRarity => 11;
}
