using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ParoxysmusDinner : Item
{
    [Constructible]
    public ParoxysmusDinner() : base(0x1E95)
    {
    }

    public override int LabelNumber => 1072086; // Paroxysmus' Dinner
}
