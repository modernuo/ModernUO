using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ThorvaldsMedallion : Item
{
    [Constructible]
    public ThorvaldsMedallion() : base(0x2AAA)
    {
        LootType = LootType.Blessed;
        Hue = 0x47F; // TODO check
    }

    public override int LabelNumber => 1074232; // Thorvald's Medallion
}
