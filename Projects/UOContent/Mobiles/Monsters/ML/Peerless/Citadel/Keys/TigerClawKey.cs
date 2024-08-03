using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator( 0 )]
public partial class TigerClawKey : PeerlessKey
{
    [Constructible]
    public TigerClawKey()
        : base( 0x2002 )
    {
        Weight = 2.0;
        Hue = 105;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1074342; // tiger claw key
}
