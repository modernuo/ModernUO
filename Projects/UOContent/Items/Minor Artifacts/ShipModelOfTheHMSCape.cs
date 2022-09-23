using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ShipModelOfTheHMSCape : Item
{
    [Constructible]
    public ShipModelOfTheHMSCape() : base(0x14F3) => Hue = 0x37B;

    public override int LabelNumber => 1063476;
}
