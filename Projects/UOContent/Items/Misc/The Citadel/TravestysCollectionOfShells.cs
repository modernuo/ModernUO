using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class TravestysCollectionOfShells : Item
{
    [Constructible]
    public TravestysCollectionOfShells() : base(0xFD3)
    {
    }

    public override int LabelNumber => 1072090; // Travesty's Collection of Shells
}
