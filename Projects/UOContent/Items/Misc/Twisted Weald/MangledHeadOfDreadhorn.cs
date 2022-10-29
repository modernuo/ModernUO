using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x3156, 0x3157)]
[SerializationGenerator(0, false)]
public partial class MangledHeadOfDreadhorn : Item
{
    [Constructible]
    public MangledHeadOfDreadhorn() : base(0x3156)
    {
    }

    public override int LabelNumber => 1072088; // The Mangled Head of Dread Horn
}
