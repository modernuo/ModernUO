using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoBloodspawn : Item
{
    [Constructible]
    public DecoBloodspawn() : base(0xF7C)
    {
        Movable = true;
        Stackable = false;
    }
}
