using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x315C, 0x315D)]
[SerializationGenerator(0, false)]
public partial class HornOfTheDreadhorn : Item
{
    [Constructible]
    public HornOfTheDreadhorn() : base(0x315C)
    {
    }

    public override int LabelNumber => 1072089; // Horn of the Dread
}
