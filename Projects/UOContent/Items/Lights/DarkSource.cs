using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DarkSource : Item
{
    [Constructible]
    public DarkSource() : base(0x1646)
    {
        Layer = Layer.TwoHanded;
        Movable = false;
    }
}
