using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoIronIngots6 : Item
{
    [Constructible]
    public DecoIronIngots6() : base(0x1BF4)
    {
        Movable = true;
        Stackable = false;
    }
}
