using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoObsidian : Item
{
    [Constructible]
    public DecoObsidian() : base(0xF89)
    {
        Movable = true;
        Stackable = false;
    }
}
