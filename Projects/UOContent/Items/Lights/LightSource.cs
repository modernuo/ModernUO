using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class LightSource : Item
{
    [Constructible]
    public LightSource() : base(0x1647)
    {
        Layer = Layer.TwoHanded;
        Movable = false;
    }
}
