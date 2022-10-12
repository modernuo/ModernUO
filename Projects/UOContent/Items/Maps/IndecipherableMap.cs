using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class IndecipherableMap : MapItem
{
    [Constructible]
    public IndecipherableMap() => Hue = Utility.RandomDouble() < 0.2 ? 0x965 : 0x961;

    public override int LabelNumber => 1070799; // indecipherable map

    public override void OnDoubleClick(Mobile from)
    {
        from.SendLocalizedMessage(1070801); // You cannot decipher this ruined map.
    }
}
