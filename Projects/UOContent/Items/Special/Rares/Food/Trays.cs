using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoTray : Item
{
    [Constructible]
    public DecoTray() : base(Utility.Random(2) + 0x991)
    {
        Movable = true;
        Stackable = false;
    }
}
