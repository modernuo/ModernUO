using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class PlagueBeastGland : Item
{
    [Constructible]
    public PlagueBeastGland() : base(0x1CEF) => Hue = 0x6;

    public override double DefaultWeight => 1.0;

    public override int LabelNumber => 1153759; // a healthy gland
}
