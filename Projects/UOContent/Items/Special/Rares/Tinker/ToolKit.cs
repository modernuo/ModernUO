using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ToolKit : Item
{
    [Constructible]
    public ToolKit() : base(Utility.Random(2) + 0x1EBA)
    {
        Movable = true;
        Stackable = false;
    }
}
