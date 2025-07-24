using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Beads : Item
{
    [Constructible]
    public Beads() : base(0x108B)
    {
    }

    public override double DefaultWeight => 1.0;
}
