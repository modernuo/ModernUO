using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GhostShipAnchor : Item
{
    [Constructible]
    public GhostShipAnchor() : base(0x14F7) => Hue = 0x47E;

    public override int LabelNumber => 1070816; // Ghost Ship Anchor
}
