using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ParoxysmusCorrodedStein : Item
{
    [Constructible]
    public ParoxysmusCorrodedStein() : base(0x9D6)
    {
    }

    public override int LabelNumber => 1072083; // Paroxysmus' Corroded Stein
}
