using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class UnfinishedBarrel : Item
{
    [Constructible]
    public UnfinishedBarrel() : base(0x1EB5)
    {
        Movable = true;
        Stackable = false;
    }
}
