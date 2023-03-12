using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class CrossbowBolts : Item
{
    [Constructible]
    public CrossbowBolts() : base(0x1BFC)
    {
        Movable = true;
        Stackable = false;
    }
}
