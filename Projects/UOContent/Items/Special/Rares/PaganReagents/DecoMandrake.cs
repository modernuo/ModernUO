using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoMandrake : Item
{
    [Constructible]
    public DecoMandrake() : base(0x18DF)
    {
        Movable = true;
        Stackable = false;
    }
}
