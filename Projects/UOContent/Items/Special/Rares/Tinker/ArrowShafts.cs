using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoArrowShafts : Item
{
    [Constructible]
    public DecoArrowShafts() : base(Utility.Random(2) + 0x1024)
    {
        Movable = true;
        Stackable = false;
    }
}
