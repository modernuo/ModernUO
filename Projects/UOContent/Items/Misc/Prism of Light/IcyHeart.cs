using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class IcyHeart : Item
{
    [Constructible]
    public IcyHeart() : base(0x24B)
    {
    }

    public override int LabelNumber => 1073162; // Icy Heart
}
