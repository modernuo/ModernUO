using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class PlayingCards2 : Item
{
    [Constructible]
    public PlayingCards2() : base(0xFA2)
    {
        Movable = true;
        Stackable = false;
    }
}
