using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SnowPileDeco : Item
{
    private static readonly int[] _types = { 0x8E2, 0x8E0, 0x8E6, 0x8E5, 0x8E3 };

    [Constructible]
    public SnowPileDeco() : this(_types.RandomElement())
    {
    }

    [Constructible]
    public SnowPileDeco(int itemid) : base(itemid) => Hue = 0x481;

    public override int LabelNumber => 1095253; // decorative snow piles
    public override double DefaultWeight => 2.0;
}
