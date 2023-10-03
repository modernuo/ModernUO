using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MeltedWax : Item
{
    [Constructible]
    public MeltedWax() : base(0x122A)
    {
        Movable = false;
        Hue = 0x835;
    }

    public override int LabelNumber => 1016492; // melted wax
}
