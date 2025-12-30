using ModernUO.Serialization;

namespace Server.Items;

[Furniture]
[Flippable(0x232A, 0x232B)]
[SerializationGenerator(0, false)]
public partial class GiftBox : BaseContainer
{
    [Constructible]
    public GiftBox() : this(Utility.RandomDyedHue())
    {
    }

    [Constructible]
    public GiftBox(int hue) : base(Utility.Random(0x232A, 2)) => Hue = hue;

    public override double DefaultWeight => 2.0;
}
