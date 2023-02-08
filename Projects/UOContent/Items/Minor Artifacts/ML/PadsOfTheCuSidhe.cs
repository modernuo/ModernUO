using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class PadsOfTheCuSidhe : FurBoots
{
    [Constructible]
    public PadsOfTheCuSidhe() : base(0x47E)
    {
    }

    public override int LabelNumber => 1075048; // Pads of the Cu Sidhe
}
