using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoBottlesOfLiquor : Item
{
    [Constructible]
    public DecoBottlesOfLiquor() : base(0x99E)
    {
        Movable = true;
        Stackable = false;
    }
}
