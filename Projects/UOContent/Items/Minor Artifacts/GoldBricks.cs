using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GoldBricks : Item
{
    [Constructible]
    public GoldBricks() : base(0x1BEB)
    {
    }

    public override int LabelNumber => 1063489;
}
