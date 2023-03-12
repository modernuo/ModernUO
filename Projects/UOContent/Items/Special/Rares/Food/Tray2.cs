using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoTray2 : Item
{
    [Constructible]
    public DecoTray2() : base(0x991)
    {
        Movable = true;
        Stackable = false;
    }
}
