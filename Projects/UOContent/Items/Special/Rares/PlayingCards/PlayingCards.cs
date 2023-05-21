using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class PlayingCards : Item
{
    [Constructible]
    public PlayingCards() : base(0xFA3)
    {
        Movable = true;
        Stackable = false;
    }
}
