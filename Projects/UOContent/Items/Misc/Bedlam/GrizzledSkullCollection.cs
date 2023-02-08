using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GrizzledSkullCollection : Item
{
    [Constructible]
    public GrizzledSkullCollection() : base(0x21FC)
    {
    }

    public override int LabelNumber => 1072116; // Grizzled Skull collection
}
