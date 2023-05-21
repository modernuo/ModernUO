using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoIronIngot : Item
{
    [Constructible]
    public DecoIronIngot() : base(0x1BEF)
    {
        Movable = true;
        Stackable = true;
    }
}
