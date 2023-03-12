using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoCrystalBall : Item
{
    [Constructible]
    public DecoCrystalBall() : base(0xE2E)
    {
        Movable = true;
        Stackable = false;
    }
}
