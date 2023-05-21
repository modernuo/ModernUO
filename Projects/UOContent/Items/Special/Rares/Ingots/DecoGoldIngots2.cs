using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoGoldIngots2 : Item
{
    [Constructible]
    public DecoGoldIngots2() : base(0x1BEB)
    {
        Movable = true;
        Stackable = false;
    }
}
